using System.Text.Json;
using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integration;

/// <summary>
/// Periodically dispatches queued outbox messages to in-process integration event handlers.
/// </summary>
public sealed class OutboxDispatcherHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox dispatch iteration failed");
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

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var pendingMessages = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(100)
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

                await publisher.Publish(deserialized, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.RetryCount += 1;
                var messageText = exception.Message;
                message.LastError = messageText.Length > 2000 ? messageText[..2000] : messageText;
                logger.LogError(exception, "Failed to dispatch outbox message {OutboxMessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
