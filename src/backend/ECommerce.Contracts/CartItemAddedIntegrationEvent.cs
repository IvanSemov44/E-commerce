namespace ECommerce.Contracts;

public sealed record CartItemAddedIntegrationEvent(
    Guid CartId,
    Guid ProductId,
    int Quantity,
    DateTime OccurredAt
) : IntegrationEvent;
