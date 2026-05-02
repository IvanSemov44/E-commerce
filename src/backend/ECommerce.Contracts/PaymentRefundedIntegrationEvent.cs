namespace ECommerce.Contracts;

public sealed record PaymentRefundedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    DateTime OccurredAt) : IntegrationEvent;
