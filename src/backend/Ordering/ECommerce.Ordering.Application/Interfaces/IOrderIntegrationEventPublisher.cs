namespace ECommerce.Ordering.Application.Interfaces;

public interface IOrderIntegrationEventPublisher
{
    Task PublishOrderPlacedAsync(
        Guid orderId,
        Guid customerId,
        IReadOnlyCollection<Guid> productIds,
        decimal totalAmount,
        CancellationToken cancellationToken = default);
}
