using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Domain.Errors;

namespace ECommerce.Ordering.Domain.ValueObjects;

public sealed class PaymentInfo : ValueObject
{
    public string PaymentReference { get; private set; } = null!;
    public string PaymentMethod { get; private set; } = null!;
    public decimal PaidAmount { get; private set; }
    public DateTime PaidAt { get; private set; }

    private PaymentInfo() { }
    private PaymentInfo(string reference, string method, decimal amount, DateTime paidAt)
    {
        PaymentReference = reference;
        PaymentMethod = method;
        PaidAmount = amount;
        PaidAt = paidAt;
    }

    public static Result<PaymentInfo> Create(string reference, string method, decimal amount, DateTime paidAt)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return Result<PaymentInfo>.Fail(OrderingErrors.PaymentRefEmpty);
        if (amount <= 0)
            return Result<PaymentInfo>.Fail(OrderingErrors.PaymentAmountInvalid);
        return Result<PaymentInfo>.Ok(new PaymentInfo(reference, method, amount, paidAt));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PaymentReference;
    }
}
