using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Shopping.Infrastructure.Integration;

public sealed class ShoppingOutboxDispatcherHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ShoppingOutboxDispatcherOptions> options,
    ILogger<ShoppingOutboxDispatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly ShoppingOutboxDispatcherOptions _options = options.Value;

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
                logger.LogError(exception, "Shopping outbox dispatch iteration failed");
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
        var dbContext = scope.ServiceProvider.GetRequiredService<ShoppingDbContext>();
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
            await ProcessMessageAsync(dbContext, integrationEventBus, message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        ShoppingDbContext dbContext,
        IIntegrationEventBus integrationEventBus,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = DeserializeIntegrationEvent(message);
            await integrationEventBus.PublishAsync(integrationEvent, cancellationToken);
            MarkMessageAsProcessed(message);
        }
        catch (Exception exception)
        {
            HandleMessageFailure(dbContext, message, exception);
        }
    }

    private static IntegrationEvent DeserializeIntegrationEvent(OutboxMessage message)
    {
        var eventType = Type.GetType(message.EventType, throwOnError: false);
        if (eventType is null)
            throw new InvalidOperationException($"Cannot resolve integration event type '{message.EventType}'");

        if (JsonSerializer.Deserialize(message.EventData, eventType) is not IntegrationEvent deserialized)
            throw new InvalidOperationException($"Failed to deserialize outbox payload for type '{message.EventType}'");

        return deserialized;
    }

    private static void MarkMessageAsProcessed(OutboxMessage message)
    {
        message.ProcessedAt = DateTime.UtcNow;
        message.NextAttemptAt = null;
        message.LastError = null;
    }

    private void HandleMessageFailure(ShoppingDbContext dbContext, OutboxMessage message, Exception exception)
    {
        message.RetryCount += 1;
        var messageText = exception.Message;
        message.LastError = messageText.Length > 2000 ? messageText[..2000] : messageText;

        if (message.RetryCount >= Math.Max(1, _options.MaxRetryAttempts))
        {
            MoveToDeadLetter(dbContext, message, exception);
            return;
        }

        var delaySeconds = ComputeBackoffDelaySeconds(message.RetryCount);
        message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        logger.LogWarning(
            exception,
            "Shopping outbox message {OutboxMessageId} failed (attempt {RetryCount}); retry scheduled at {NextAttemptAt}",
            message.Id,
            message.RetryCount,
            message.NextAttemptAt);
    }

    private void MoveToDeadLetter(ShoppingDbContext dbContext, OutboxMessage message, Exception exception)
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
            "Shopping outbox message {OutboxMessageId} moved to dead-letter after {RetryCount} failed attempts",
            message.Id,
            message.RetryCount);
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
