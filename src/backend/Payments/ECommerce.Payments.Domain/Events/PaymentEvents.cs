using ECommerce.SharedKernel.Domain;

namespace ECommerce.Payments.Domain.Events;

public sealed record PaymentProcessedEvent(Guid PaymentId, Guid OrderId, decimal Amount, string PaymentMethod) : DomainEventBase;
public sealed record PaymentFailedEvent(Guid PaymentId, Guid OrderId, string Reason) : DomainEventBase;
public sealed record PaymentRefundedEvent(Guid PaymentId, Guid OrderId, decimal Amount) : DomainEventBase;
