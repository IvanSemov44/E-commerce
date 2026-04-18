namespace ECommerce.Shopping.Application.Interfaces;

public interface ICartIntegrationEventPublisher
{
    Task PublishCartItemAddedAsync(
        Guid cartId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task PublishCartItemQuantityUpdatedAsync(
        Guid cartId,
        Guid productId,
        int newQuantity,
        CancellationToken cancellationToken = default);

    Task PublishCartClearedAsync(
        Guid cartId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
