using System.Text.Json;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Catalog.Infrastructure.Integration;

/// <summary>
/// Polls catalog.outbox_messages and publishes them to the integration event bus.
/// Runs independently of the shared OutboxDispatcherHostedService so the Catalog BC
/// owns its full outbox lifecycle without touching IntegrationPersistenceDbContext.
/// </summary>
public sealed class CatalogOutboxDispatcherHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxDispatcherOptions> options,
    ILogger<CatalogOutboxDispatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly OutboxDispatcherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Catalog outbox dispatch iteration failed");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task DispatchOnceAsync(CancellationToken cancellationToken)
        => DispatchBatchAsync(cancellationToken);

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m =>
                m.ProcessedAt == null &&
                !m.IsDeadLettered &&
                (m.NextAttemptAt == null || m.NextAttemptAt <= utcNow))
            .OrderBy(m => m.CreatedAt)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        foreach (var message in pendingMessages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType, throwOnError: false);
                if (eventType is null)
                    throw new InvalidOperationException($"Cannot resolve integration event type '{message.EventType}'");

                if (JsonSerializer.Deserialize(message.EventData, eventType) is not IntegrationEvent deserialized)
                    throw new InvalidOperationException($"Failed to deserialize outbox payload for type '{message.EventType}'");

                await integrationEventBus.PublishAsync(deserialized, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.NextAttemptAt = null;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.RetryCount += 1;
                var messageText = exception.Message;
                message.LastError = messageText.Length > 2000 ? messageText[..2000] : messageText;

                if (message.RetryCount >= Math.Max(1, _options.MaxRetryAttempts))
                {
                    message.IsDeadLettered = true;
                    message.DeadLetteredAt = DateTime.UtcNow;
                    message.NextAttemptAt = null;

                    dbContext.DeadLetterMessages.Add(new DeadLetterMessage
                    {
                        Id = Guid.NewGuid(),
                        OutboxMessageId = message.Id,
                        IdempotencyKey = message.IdempotencyKey,
                        EventType = message.EventType,
                        EventData = message.EventData,
                        RetryCount = message.RetryCount,
                        LastError = message.LastError,
                        FailedAt = message.DeadLetteredAt.Value
                    });

                    logger.LogError(
                        exception,
                        "Catalog outbox message {OutboxMessageId} moved to dead-letter after {RetryCount} failed attempts",
                        message.Id,
                        message.RetryCount);
                }
                else
                {
                    var delaySeconds = ComputeBackoffDelaySeconds(message.RetryCount);
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                    logger.LogWarning(
                        exception,
                        "Catalog outbox message {OutboxMessageId} failed (attempt {RetryCount}); retry scheduled at {NextAttemptAt}",
                        message.Id,
                        message.RetryCount,
                        message.NextAttemptAt);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private int ComputeBackoffDelaySeconds(int retryCount)
    {
        var safeRetryCount = Math.Max(1, retryCount);
        var baseDelay = Math.Max(1, _options.BaseRetryDelaySeconds);
        var maxDelay = Math.Max(baseDelay, _options.MaxRetryDelaySeconds);
        var exponentialDelay = baseDelay * Math.Pow(2, safeRetryCount - 1);
        return (int)Math.Min(maxDelay, exponentialDelay);
    }
}
