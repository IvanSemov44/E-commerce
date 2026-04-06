using ECommerce.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integration;

public sealed class ProductProjectionUpdatedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<ProductProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<ProductProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductProjectionUpdatedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => mediator.Publish(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed product projection integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class ProductImageProjectionUpdatedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<ProductImageProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<ProductImageProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductImageProjectionUpdatedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => mediator.Publish(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed product image projection integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class PromoCodeProjectionUpdatedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<PromoCodeProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<PromoCodeProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PromoCodeProjectionUpdatedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => mediator.Publish(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed promo projection integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class AddressProjectionUpdatedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<AddressProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<AddressProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<AddressProjectionUpdatedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => mediator.Publish(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed address projection integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class InventoryStockProjectionUpdatedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IPublisher mediator,
    ILogger<InventoryStockProjectionUpdatedIntegrationEventConsumer> logger)
    : IConsumer<InventoryStockProjectionUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InventoryStockProjectionUpdatedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => mediator.Publish(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed inventory projection integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}
