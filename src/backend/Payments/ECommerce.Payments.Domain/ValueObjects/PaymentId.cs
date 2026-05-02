namespace ECommerce.Payments.Domain.ValueObjects;

public readonly record struct PaymentId(string Value)
{
    public static PaymentId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
