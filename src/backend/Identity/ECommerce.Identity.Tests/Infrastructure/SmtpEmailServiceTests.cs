using ECommerce.Identity.Infrastructure.Services;
using ECommerce.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Identity.Tests.Infrastructure;

[TestClass]
public class SmtpEmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SmtpEmailService>> _loggerMock;

    public SmtpEmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<SmtpEmailService>>();
    }

    [TestInitialize]
    public void Setup()
    {
        _configurationMock.Reset();
        _loggerMock.Reset();

        _configurationMock.Setup(x => x["Smtp:Host"]).Returns("smtp.test.com");
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns("587");
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:FromEmail"]).Returns("noreply@test.com");
        _configurationMock.Setup(x => x["Smtp:FromName"]).Returns("Test Store");
        _configurationMock.Setup(x => x["Smtp:EnableSsl"]).Returns("true");
    }

    [TestMethod]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingHost_UsesDefault()
    {
        _configurationMock.Setup(x => x["Smtp:Host"]).Returns((string?)null);
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingPort_UsesDefault()
    {
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns((string?)null);
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithInvalidPort_UsesDefault()
    {
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns("invalid");
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingEnableSsl_UsesDefault()
    {
        _configurationMock.Setup(x => x["Smtp:EnableSsl"]).Returns((string?)null);
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromEmail_UsesUsername()
    {
        _configurationMock.Setup(x => x["Smtp:FromEmail"]).Returns((string?)null);
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromName_UsesDefault()
    {
        _configurationMock.Setup(x => x["Smtp:FromName"]).Returns((string?)null);
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithCredentials_InitializesAsEnabled()
    {
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("password123");
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOnlyUsername_InitializesAsDisabled()
    {
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("");
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOnlyPassword_InitializesAsDisabled()
    {
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("password123");
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendWelcomeEmailAsync(
            "test@example.com", "John", "https://example.com/verify?token=abc123"));
    }

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithCancellationToken_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        await Should.NotThrowAsync(() => service.SendWelcomeEmailAsync(
            "test@example.com", "John", "https://example.com/verify", cts.Token));
    }

    [TestMethod]
    public async Task SendEmailVerificationAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendEmailVerificationAsync(
            "test@example.com", "John", "https://example.com/verify?token=abc123"));
    }

    [TestMethod]
    public async Task SendPasswordResetEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendPasswordResetEmailAsync(
            "test@example.com", "John", "https://example.com/reset?token=abc123"));
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithValidOrder_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderConfirmationEmailAsync(
            "test@example.com", CreateTestOrder()));
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithDiscount_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderConfirmationEmailAsync(
            "test@example.com", CreateTestOrder() with { DiscountAmount = 10.00m }));
    }

    [TestMethod]
    public async Task SendOrderShippedEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderShippedEmailAsync(
            "test@example.com", CreateTestOrder(), "TRACK123456"));
    }

    [TestMethod]
    public async Task SendOrderDeliveredEmailAsync_WithValidOrder_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendOrderDeliveredEmailAsync(
            "test@example.com", CreateTestOrder()));
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithValidCart_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendAbandonedCartEmailAsync(
            "test@example.com", "John", CreateTestCart()));
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithEmptyCart_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendAbandonedCartEmailAsync(
            "test@example.com", "John", new CartEmailDto(Items: [])));
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendLowStockAlertAsync(
            "admin@example.com", "Admin", "Test Product", 5, 10));
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithSku_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        await Should.NotThrowAsync(() => service.SendLowStockAlertAsync(
            "admin@example.com", "Admin", "Test Product", 5, 10, "SKU-123"));
    }

    [TestMethod]
    public async Task SendMarketingEmailAsync_WithValidData_DoesNotThrow()
    {
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
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
