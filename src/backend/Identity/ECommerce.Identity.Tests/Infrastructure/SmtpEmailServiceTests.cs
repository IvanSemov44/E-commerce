using ECommerce.Identity.Infrastructure.Services;
using ECommerce.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Identity.Tests.Infrastructure;

[TestClass]
public class SmtpEmailServiceTests
{
    private Mock<IConfiguration> _config = null!;
    private Mock<ILogger<SmtpEmailService>> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new Mock<IConfiguration>();
        _logger = new Mock<ILogger<SmtpEmailService>>();

        _config.Setup(x => x["Smtp:Host"]).Returns("smtp.test.com");
        _config.Setup(x => x["Smtp:Port"]).Returns("587");
        _config.Setup(x => x["Smtp:Username"]).Returns("");
        _config.Setup(x => x["Smtp:Password"]).Returns("");
        _config.Setup(x => x["Smtp:FromEmail"]).Returns("noreply@test.com");
        _config.Setup(x => x["Smtp:FromName"]).Returns("Test Store");
        _config.Setup(x => x["Smtp:EnableSsl"]).Returns("true");
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NoCredentials_LogsDisabledWarning()
    {
        _ = new SmtpEmailService(_config.Object, _logger.Object);

        VerifyLog(LogLevel.Warning, "SMTP credentials not configured");
    }

    [TestMethod]
    public void Constructor_WithCredentials_LogsInitialized()
    {
        _config.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        _config.Setup(x => x["Smtp:Password"]).Returns("password123");

        _ = new SmtpEmailService(_config.Object, _logger.Object);

        VerifyLog(LogLevel.Information, "SMTP Email Service initialized");
    }

    // ── Send* methods (disabled state — no network calls made) ───────────────

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendWelcomeEmailAsync("test@example.com", "John", "https://example.com/verify");

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendEmailVerificationAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendEmailVerificationAsync("test@example.com", "John", "https://example.com/verify");

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendPasswordResetEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendPasswordResetEmailAsync("test@example.com", "John", "https://example.com/reset");

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendOrderConfirmationEmailAsync("test@example.com", CreateTestOrder());

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendOrderShippedEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendOrderShippedEmailAsync("test@example.com", CreateTestOrder(), "TRACK123456");

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendOrderDeliveredEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendOrderDeliveredEmailAsync("test@example.com", CreateTestOrder());

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendAbandonedCartEmailAsync("test@example.com", "John", CreateTestCart());

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendLowStockAlertAsync("admin@example.com", "Admin", "Test Product", 5, 10);

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
    }

    [TestMethod]
    public async Task SendMarketingEmailAsync_WhenDisabled_LogsWarning()
    {
        var svc = new SmtpEmailService(_config.Object, _logger.Object);

        await svc.SendMarketingEmailAsync("customer@example.com", "John", "Special Offer!", "<p>Content</p>");

        VerifyLog(LogLevel.Warning, "Email sending is disabled");
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
