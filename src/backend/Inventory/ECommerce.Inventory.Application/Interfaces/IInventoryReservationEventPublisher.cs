namespace ECommerce.Inventory.Application.Interfaces;

public interface IInventoryReservationEventPublisher
{
    Task PublishInventoryReservedAsync(
        Guid orderId,
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<int> quantities,
        CancellationToken cancellationToken = default);

    Task PublishInventoryReservationFailedAsync(
        Guid orderId,
        Guid productId,
        string reason,
        CancellationToken cancellationToken = default);
}
