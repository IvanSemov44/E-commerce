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

public sealed class OrderPlacedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IOrderFulfillmentSagaService sagaService,
    ILogger<OrderPlacedIntegrationEventConsumer> logger)
    : IConsumer<OrderPlacedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => sagaService.StartAsync(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed order placed integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class InventoryReservedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IOrderFulfillmentSagaService sagaService,
    ILogger<InventoryReservedIntegrationEventConsumer> logger)
    : IConsumer<InventoryReservedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InventoryReservedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => sagaService.HandleInventoryReservedAsync(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed inventory reserved integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}

public sealed class InventoryReservationFailedIntegrationEventConsumer(
    InboxIdempotencyProcessor inbox,
    IOrderFulfillmentSagaService sagaService,
    ILogger<InventoryReservationFailedIntegrationEventConsumer> logger)
    : IConsumer<InventoryReservationFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<InventoryReservationFailedIntegrationEvent> context)
    {
        await inbox.ExecuteAsync(
            context.Message,
            ct => sagaService.HandleInventoryReservationFailedAsync(context.Message, ct),
            context.CancellationToken);

        logger.LogDebug("Processed inventory reservation failed integration event {IdempotencyKey}", context.Message.IdempotencyKey);
    }
}
