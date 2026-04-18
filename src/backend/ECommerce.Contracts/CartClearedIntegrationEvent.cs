namespace ECommerce.Contracts;

public sealed record CartClearedIntegrationEvent(
    Guid CartId,
    Guid UserId,
    DateTime OccurredAt
) : IntegrationEvent;
