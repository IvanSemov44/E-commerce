using ECommerce.Contracts;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Infrastructure.Integration;

namespace ECommerce.Shopping.Infrastructure.Services;

public sealed class CartIntegrationEventPublisher(IShoppingOutboxEventWriter outbox)
    : ICartIntegrationEventPublisher
{
    public Task PublishCartItemAddedAsync(
        Guid cartId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new CartItemAddedIntegrationEvent(
            cartId,
            productId,
            quantity,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }

    public Task PublishCartItemQuantityUpdatedAsync(
        Guid cartId,
        Guid productId,
        int newQuantity,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new CartItemQuantityUpdatedIntegrationEvent(
            cartId,
            productId,
            newQuantity,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }

    public Task PublishCartClearedAsync(
        Guid cartId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var integrationEvent = new CartClearedIntegrationEvent(
            cartId,
            userId,
            DateTime.UtcNow);

        return outbox.EnqueueAsync(integrationEvent, cancellationToken);
    }
}
