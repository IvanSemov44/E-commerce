namespace ECommerce.Contracts;

public sealed record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime OccurredAt) : IntegrationEvent;
