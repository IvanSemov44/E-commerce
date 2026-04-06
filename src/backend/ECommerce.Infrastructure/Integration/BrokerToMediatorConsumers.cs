using ECommerce.Contracts;
using ECommerce.Infrastructure.Data;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integration;

public sealed class ProductProjectionUpdatedIntegrationEventConsumer(IPublisher mediator)
    : IConsumer<ProductProjectionUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ProductProjectionUpdatedIntegrationEvent> context)
        => mediator.Publish(context.Message, context.CancellationToken);
}

public sealed class ProductImageProjectionUpdatedIntegrationEventConsumer(IPublisher mediator)
    : IConsumer<ProductImageProjectionUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ProductImageProjectionUpdatedIntegrationEvent> context)
        => mediator.Publish(context.Message, context.CancellationToken);
}

public sealed class PromoCodeProjectionUpdatedIntegrationEventConsumer(IPublisher mediator)
    : IConsumer<PromoCodeProjectionUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PromoCodeProjectionUpdatedIntegrationEvent> context)
        => mediator.Publish(context.Message, context.CancellationToken);
}

public sealed class AddressProjectionUpdatedIntegrationEventConsumer(IPublisher mediator)
    : IConsumer<AddressProjectionUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<AddressProjectionUpdatedIntegrationEvent> context)
        => mediator.Publish(context.Message, context.CancellationToken);
}

public sealed class InventoryStockProjectionUpdatedIntegrationEventConsumer(
    AppDbContext dbContext,
    IPublisher mediator,
    ILogger<InventoryStockProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<InventoryStockProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InventoryStockProjectionUpdatedIntegrationEvent> context)
    {
        var message = context.Message;

        var existing = await dbContext.InboxMessages
            .SingleOrDefaultAsync(x => x.IdempotencyKey == message.IdempotencyKey, context.CancellationToken);

        if (existing?.ProcessedAt is not null)
        {
            logger.LogInformation(
                "Skipping already processed inventory integration event {IdempotencyKey}",
                message.IdempotencyKey);
            return;
        }

        if (existing is null)
        {
            existing = new InboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = message.IdempotencyKey,
                EventType = typeof(InventoryStockProjectionUpdatedIntegrationEvent).FullName ?? nameof(InventoryStockProjectionUpdatedIntegrationEvent),
                ReceivedAt = DateTime.UtcNow
            };

            dbContext.InboxMessages.Add(existing);

            try
            {
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }
            catch (DbUpdateException)
            {
                // Competing duplicate delivery inserted first; re-read current state.
                existing = await dbContext.InboxMessages
                    .SingleAsync(x => x.IdempotencyKey == message.IdempotencyKey, context.CancellationToken);

                if (existing.ProcessedAt is not null)
                    return;
            }
        }

        try
        {
            await mediator.Publish(message, context.CancellationToken);

            existing.AttemptCount += 1;
            existing.ProcessedAt = DateTime.UtcNow;
            existing.LastError = null;
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception exception)
        {
            existing.AttemptCount += 1;
            var messageText = exception.Message;
            existing.LastError = messageText.Length > 2000 ? messageText[..2000] : messageText;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            logger.LogError(
                exception,
                "Failed to process inventory integration event {IdempotencyKey}",
                message.IdempotencyKey);

            throw;
        }
    }
}
