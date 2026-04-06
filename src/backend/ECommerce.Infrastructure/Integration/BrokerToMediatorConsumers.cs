using ECommerce.Contracts;
using MassTransit;
using MediatR;

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

public sealed class InventoryStockProjectionUpdatedIntegrationEventConsumer(IPublisher mediator)
    : IConsumer<InventoryStockProjectionUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InventoryStockProjectionUpdatedIntegrationEvent> context)
        => mediator.Publish(context.Message, context.CancellationToken);
}
