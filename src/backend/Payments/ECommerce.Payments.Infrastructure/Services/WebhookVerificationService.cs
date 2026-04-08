using System.Security.Cryptography;
using System.Text;
using ECommerce.Payments.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Payments.Infrastructure.Services;

public class WebhookVerificationService(IConfiguration configuration) : IWebhookVerificationService
{
    private readonly string _secret = configuration["PaymentWebhook:Secret"]
        ?? throw new InvalidOperationException("Webhook secret not configured");

    public bool VerifySignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
