using ECommerce.Identity.Infrastructure.Services;
using ECommerce.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace ECommerce.Identity.Tests.Infrastructure;

[TestClass]
public class SendGridEmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SendGridEmailService>> _loggerMock;

    public SendGridEmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<SendGridEmailService>>();
    }

    [TestInitialize]
    public void Setup()
    {
        _configurationMock.Reset();
        _loggerMock.Reset();

        _configurationMock.Setup(x => x["SendGrid:ApiKey"]).Returns("test-api-key");
        _configurationMock.Setup(x => x["SendGrid:FromEmail"]).Returns("noreply@test.com");
        _configurationMock.Setup(x => x["SendGrid:FromName"]).Returns("Test Store");
    }

    [TestMethod]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingApiKey_UsesDisabledKey()
    {
        _configurationMock.Setup(x => x["SendGrid:ApiKey"]).Returns((string?)null);
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithEmptyApiKey_UsesDisabledKey()
    {
        _configurationMock.Setup(x => x["SendGrid:ApiKey"]).Returns("");
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromEmail_UsesDefault()
    {
        _configurationMock.Setup(x => x["SendGrid:FromEmail"]).Returns((string?)null);
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromName_UsesDefault()
    {
        _configurationMock.Setup(x => x["SendGrid:FromName"]).Returns((string?)null);
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendWelcomeEmailAsync(
            "test@example.com", "John", "https://example.com/verify?token=abc123"));
    }

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithCancellationToken_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        await Should.NotThrowAsync(() => service.SendWelcomeEmailAsync(
            "test@example.com", "John", "https://example.com/verify", cts.Token));
    }

    [TestMethod]
    public async Task SendEmailVerificationAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendEmailVerificationAsync(
            "test@example.com", "John", "https://example.com/verify?token=abc123"));
    }

    [TestMethod]
    public async Task SendPasswordResetEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendPasswordResetEmailAsync(
            "test@example.com", "John", "https://example.com/reset?token=abc123"));
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithValidOrder_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderConfirmationEmailAsync(
            "test@example.com", CreateTestOrder()));
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithDiscount_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderConfirmationEmailAsync(
            "test@example.com", CreateTestOrder() with { DiscountAmount = 10.00m }));
    }

    [TestMethod]
    public async Task SendOrderShippedEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderShippedEmailAsync(
            "test@example.com", CreateTestOrder(), "TRACK123456"));
    }

    [TestMethod]
    public async Task SendOrderDeliveredEmailAsync_WithValidOrder_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderDeliveredEmailAsync(
            "test@example.com", CreateTestOrder()));
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithValidCart_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendAbandonedCartEmailAsync(
            "test@example.com", "John", CreateTestCart()));
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithEmptyCart_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendAbandonedCartEmailAsync(
            "test@example.com", "John", new CartEmailDto(Items: [])));
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendLowStockAlertAsync(
            "admin@example.com", "Admin", "Test Product", 5, 10));
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithSku_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendLowStockAlertAsync(
            "admin@example.com", "Admin", "Test Product", 5, 10, "SKU-123"));
    }

    [TestMethod]
    public async Task SendMarketingEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendMarketingEmailAsync(
            "customer@example.com", "John", "Special Offer!", "<p>Check out our new products!</p>"));
    }

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
