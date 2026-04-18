namespace ECommerce.Contracts;

public sealed record CartItemQuantityUpdatedIntegrationEvent(
    Guid CartId,
    Guid ProductId,
    int NewQuantity,
    DateTime OccurredAt
) : IntegrationEvent;
