using ECommerce.Identity.Infrastructure.Services;
using ECommerce.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Identity.Tests.Infrastructure;

[TestClass]
public class SendGridEmailServiceTests
{
    private Mock<IConfiguration> _config = null!;
    private Mock<ILogger<SendGridEmailService>> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new Mock<IConfiguration>();
        _logger = new Mock<ILogger<SendGridEmailService>>();

        _config.Setup(x => x["SendGrid:ApiKey"]).Returns("test-api-key");
        _config.Setup(x => x["SendGrid:FromEmail"]).Returns("noreply@test.com");
        _config.Setup(x => x["SendGrid:FromName"]).Returns("Test Store");
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_WithEmptyApiKey_LogsWarning()
    {
        _config.Setup(x => x["SendGrid:ApiKey"]).Returns("");

        _ = new SendGridEmailService(_config.Object, _logger.Object);

        VerifyLog(LogLevel.Warning, "SendGrid API key not configured");
    }

    [TestMethod]
    public void Constructor_WithValidApiKey_DoesNotLogWarning()
    {
        _ = new SendGridEmailService(_config.Object, _logger.Object);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ── Send* methods (invalid key → SendGrid rejects → LogError) ────────────
    //
    // These tests make a real HTTP call; SendGrid always rejects "test-api-key"
    // with 401 (or throws on network failure), so the service logs an error.
    // They verify that the error-handling path runs and does not propagate exceptions.

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendWelcomeEmailAsync("test@example.com", "John", "https://example.com/verify");

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendEmailVerificationAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendEmailVerificationAsync("test@example.com", "John", "https://example.com/verify");

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendPasswordResetEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendPasswordResetEmailAsync("test@example.com", "John", "https://example.com/reset");

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendOrderConfirmationEmailAsync("test@example.com", CreateTestOrder());

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendOrderShippedEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendOrderShippedEmailAsync("test@example.com", CreateTestOrder(), "TRACK123456");

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendOrderDeliveredEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendOrderDeliveredEmailAsync("test@example.com", CreateTestOrder());

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendAbandonedCartEmailAsync("test@example.com", "John", CreateTestCart());

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendLowStockAlertAsync("admin@example.com", "Admin", "Test Product", 5, 10);

        VerifyLogError();
    }

    [TestMethod]
    public async Task SendMarketingEmailAsync_WithInvalidKey_LogsError()
    {
        var svc = new SendGridEmailService(_config.Object, _logger.Object);

        await svc.SendMarketingEmailAsync("customer@example.com", "John", "Special Offer!", "<p>Content</p>");

        VerifyLogError();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void VerifyLog(LogLevel level, string messageFragment) =>
        _logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(messageFragment)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    private void VerifyLogError() =>
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    private static OrderEmailDto CreateTestOrder() =>
        new(
            OrderNumber: "ORD-123",
            CreatedAt: DateTime.UtcNow,
            Status: "Pending",
            Subtotal: 100.00m,
            DiscountAmount: 0,
            ShippingAmount: 10.00m,
            TaxAmount: 8.00m,
            TotalAmount: 118.00m,
            Items: [new OrderItemEmailDto("Test Product", 2, 50.00m, 100.00m)],
            ShippingAddress: new AddressEmailDto("John", "Doe", "123 Main St", null, "New York", "NY", "10001", "USA"));

    private static CartEmailDto CreateTestCart() =>
        new(Items:
        [
            new CartItemEmailDto("Test Product", 2, 29.99m),
            new CartItemEmailDto("Another Product", 1, 49.99m)
        ]);
}
