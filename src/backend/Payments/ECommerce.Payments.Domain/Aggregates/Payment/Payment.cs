using ECommerce.Payments.Domain.Enums;
using ECommerce.Payments.Domain.Errors;
using ECommerce.Payments.Domain.Events;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Payments.Domain.Aggregates.Payment;

public sealed class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public string PaymentMethod { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public string? PaymentIntentId { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private Payment() { }

    public static Payment Initiate(Guid orderId, string paymentMethod, decimal amount, string currency = "USD")
        => new()
        {
            OrderId = orderId,
            PaymentMethod = paymentMethod,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Processing,
        };

    public Result MarkPaid(string paymentIntentId, string transactionId)
    {
        if (Status != PaymentStatus.Processing)
            return Result.Fail(PaymentErrors.InvalidTransition);

        PaymentIntentId = paymentIntentId;
        TransactionId = transactionId;
        Status = PaymentStatus.Paid;
        ProcessedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentProcessedEvent(Id, OrderId, Amount, PaymentMethod));
        return Result.Ok();
    }

    public Result MarkFailed(string failureReason)
    {
        if (Status != PaymentStatus.Processing)
            return Result.Fail(PaymentErrors.InvalidTransition);

        FailureReason = failureReason;
        Status = PaymentStatus.Failed;
        ProcessedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(Id, OrderId, failureReason));
        return Result.Ok();
    }

    public Result Refund()
    {
        if (Status != PaymentStatus.Paid)
            return Result.Fail(PaymentErrors.InvalidRefund);

        Status = PaymentStatus.Refunded;
        AddDomainEvent(new PaymentRefundedEvent(Id, OrderId, Amount));
        return Result.Ok();
    }
}
