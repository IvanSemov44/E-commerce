namespace ECommerce.Payments.Application.Interfaces;

public record GatewayChargeResult(
    bool Succeeded,
    string PaymentIntentId,
    string TransactionId,
    string ProviderName,
    string? FailureReason);

public interface IPaymentGateway
{
    Task<GatewayChargeResult> ChargeAsync(string paymentMethod, decimal amount, CancellationToken ct = default);
}
