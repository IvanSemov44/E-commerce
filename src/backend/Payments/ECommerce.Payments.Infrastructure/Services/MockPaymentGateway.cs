using ECommerce.Payments.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Payments.Infrastructure.Services;

public sealed class MockPaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public Task<GatewayChargeResult> ChargeAsync(string paymentMethod, decimal amount, CancellationToken ct = default)
    {
        var intentId = GeneratePaymentIntentId(paymentMethod);
        var transactionId = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        var providerName = GetProviderName(paymentMethod);

        if (ShouldSimulateFailure())
            return Task.FromResult(new GatewayChargeResult(false, intentId, string.Empty, providerName, "Payment declined"));

        return Task.FromResult(new GatewayChargeResult(true, intentId, transactionId, providerName, null));
    }

    private bool ShouldSimulateFailure()
    {
        if (!configuration.GetValue<bool>("Payment:SimulateFailures", false))
            return false;

        return new Random().Next(0, 100) < 5;
    }

    private static string GeneratePaymentIntentId(string method)
    {
        var prefix = method switch
        {
            "stripe" => "pi_",
            "paypal" => "ppi_",
            "apple_pay" => "ap_",
            "google_pay" => "gp_",
            _ => "pi_"
        };
        return string.Concat(prefix, Guid.NewGuid().ToString("N").AsSpan(0, 20));
    }

    private static string GetProviderName(string method) => method switch
    {
        "stripe" => "Stripe",
        "paypal" => "PayPal",
        "apple_pay" => "Apple Pay",
        "google_pay" => "Google Pay",
        "credit_card" => "Credit Card",
        "debit_card" => "Debit Card",
        _ => "Unknown"
    };
}
