namespace ECommerce.Contracts;

public sealed record PaymentProcessedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string PaymentMethod,
    DateTime OccurredAt) : IntegrationEvent;
