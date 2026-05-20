using ECommerce.Payments.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Payments.Tests.Infrastructure;

[TestClass]
public class WebhookVerificationServiceTests
{
    private IConfiguration _configuration = null!;
    private WebhookVerificationService _service = null!;
    private const string TestSecret = "test-webhook-secret-key-12345";

    [TestInitialize]
    public void Setup()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "PaymentWebhook:Secret", TestSecret }
        });
        _configuration = configBuilder.Build();
        _service = new WebhookVerificationService(_configuration);
    }

    [TestMethod]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";
        var validSignature = ComputeHmacSha256(payload, TestSecret);

        _service.VerifySignature(payload, validSignature).ShouldBeTrue();
    }

    [TestMethod]
    public void VerifySignature_WithInvalidSignature_ReturnsFalse()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";

        _service.VerifySignature(payload, "invalid-signature-hash").ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_WithEmptySignature_ReturnsFalse()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";

        _service.VerifySignature(payload, "").ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_WithNullSignature_ReturnsFalse()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";

        _service.VerifySignature(payload, null!).ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_WithModifiedPayload_ReturnsFalse()
    {
        const string originalPayload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";
        const string modifiedPayload = "{\"event\":\"payment.success\",\"orderId\":\"999\"}";
        var signatureForOriginal = ComputeHmacSha256(originalPayload, TestSecret);

        _service.VerifySignature(modifiedPayload, signatureForOriginal).ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_IsCaseSensitiveForPayload()
    {
        const string payload = "{\"event\":\"payment.success\"}";
        const string differentCasePayload = "{\"EVENT\":\"PAYMENT.SUCCESS\"}";
        var signature = ComputeHmacSha256(payload, TestSecret);

        _service.VerifySignature(differentCasePayload, signature).ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_WithDifferentSecret_ReturnsFalse()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";
        var signatureWithDifferentSecret = ComputeHmacSha256(payload, "different-secret-key");

        _service.VerifySignature(payload, signatureWithDifferentSecret).ShouldBeFalse();
    }

    [TestMethod]
    public void VerifySignature_WithUppercaseSignature_ReturnsTrue()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";
        var uppercaseSignature = ComputeHmacSha256(payload, TestSecret).ToUpperInvariant();

        _service.VerifySignature(payload, uppercaseSignature).ShouldBeTrue();
    }

    [TestMethod]
    public void VerifySignature_IsIdempotent()
    {
        const string payload = "{\"event\":\"payment.success\",\"orderId\":\"123\"}";
        var signature = ComputeHmacSha256(payload, TestSecret);

        _service.VerifySignature(payload, signature).ShouldBeTrue();
        _service.VerifySignature(payload, signature).ShouldBeTrue();
        _service.VerifySignature(payload, signature).ShouldBeTrue();
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
