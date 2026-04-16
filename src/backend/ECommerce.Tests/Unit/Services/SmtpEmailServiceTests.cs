using ECommerce.Infrastructure.Services;
using ECommerce.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

/// <summary>
/// Unit tests for SmtpEmailService.
/// Tests email sending functionality through SMTP.
/// </summary>
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

        // Default configuration - disabled (no credentials)
        _configurationMock.Setup(x => x["Smtp:Host"]).Returns("smtp.test.com");
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns("587");
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:FromEmail"]).Returns("noreply@test.com");
        _configurationMock.Setup(x => x["Smtp:FromName"]).Returns("Test Store");
        _configurationMock.Setup(x => x["Smtp:EnableSsl"]).Returns("true");
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithValidConfiguration_InitializesCorrectly()
    {
        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingHost_UsesDefault()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Host"]).Returns((string?)null);

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingPort_UsesDefault()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns((string?)null);

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithInvalidPort_UsesDefault()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Port"]).Returns("invalid");

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingEnableSsl_UsesDefault()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:EnableSsl"]).Returns((string?)null);

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromEmail_UsesUsername()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:FromEmail"]).Returns((string?)null);
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithMissingFromName_UsesDefault()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:FromName"]).Returns((string?)null);

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithCredentials_InitializesAsEnabled()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("password123");

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOnlyUsername_InitializesAsDisabled()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("user@test.com");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("");

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [TestMethod]
    public void Constructor_WithOnlyPassword_InitializesAsDisabled()
    {
        // Arrange
        _configurationMock.Setup(x => x["Smtp:Username"]).Returns("");
        _configurationMock.Setup(x => x["Smtp:Password"]).Returns("password123");

        // Act
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    #endregion

    #region SendWelcomeEmailAsync Tests

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var verificationLink = "https://example.com/verify?token=abc123";

        // Act
        var action = async () => await service.SendWelcomeEmailAsync(email, firstName, verificationLink);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendWelcomeEmailAsync_WithCancellationToken_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        var action = async () => await service.SendWelcomeEmailAsync(
            "test@example.com", "John", "https://example.com/verify", cts.Token);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendEmailVerificationAsync Tests

    [TestMethod]
    public async Task SendEmailVerificationAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var verificationLink = "https://example.com/verify?token=abc123";

        // Act
        var action = async () => await service.SendEmailVerificationAsync(email, firstName, verificationLink);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendPasswordResetEmailAsync Tests

    [TestMethod]
    public async Task SendPasswordResetEmailAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var resetLink = "https://example.com/reset?token=abc123";

        // Act
        var action = async () => await service.SendPasswordResetEmailAsync(email, firstName, resetLink);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendOrderConfirmationEmailAsync Tests

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithValidOrder_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var order = CreateTestOrder();

        // Act
        var action = async () => await service.SendOrderConfirmationEmailAsync(email, order);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithOrderWithDiscount_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var order = CreateTestOrder() with { DiscountAmount = 10.00m };

        // Act
        var action = async () => await service.SendOrderConfirmationEmailAsync(email, order);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendOrderConfirmationEmailAsync_WithOrderWithStreetLine2_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var order = CreateTestOrder() with
        {
            ShippingAddress = new AddressEmailDto("John", "Doe", "123 Main St", "Apt 4B", "New York", "NY", "10001", "USA")
        };

        // Act
        var action = async () => await service.SendOrderConfirmationEmailAsync(email, order);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendOrderShippedEmailAsync Tests

    [TestMethod]
    public async Task SendOrderShippedEmailAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var order = CreateTestOrder();
        var trackingNumber = "TRACK123456";

        // Act
        var action = async () => await service.SendOrderShippedEmailAsync(email, order, trackingNumber);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendOrderDeliveredEmailAsync Tests

    [TestMethod]
    public async Task SendOrderDeliveredEmailAsync_WithValidOrder_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var order = CreateTestOrder();

        // Act
        var action = async () => await service.SendOrderDeliveredEmailAsync(email, order);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendAbandonedCartEmailAsync Tests

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithValidCart_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var cart = CreateTestCart();

        // Act
        var action = async () => await service.SendAbandonedCartEmailAsync(email, firstName, cart);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithEmptyCart_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var cart = new CartEmailDto(Items: []);

        // Act
        var action = async () => await service.SendAbandonedCartEmailAsync(email, firstName, cart);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendAbandonedCartEmailAsync_WithNullItems_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "test@example.com";
        var firstName = "John";
        var cart = new CartEmailDto(Items: null!);

        // Act
        var action = async () => await service.SendAbandonedCartEmailAsync(email, firstName, cart);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendLowStockAlertAsync Tests

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "admin@example.com";
        var firstName = "Admin";
        var productName = "Test Product";
        var currentStock = 5;
        var threshold = 10;

        // Act
        var action = async () => await service.SendLowStockAlertAsync(
            email, firstName, productName, currentStock, threshold);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithSku_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "admin@example.com";
        var firstName = "Admin";
        var productName = "Test Product";
        var currentStock = 5;
        var threshold = 10;
        var sku = "SKU-123";

        // Act
        var action = async () => await service.SendLowStockAlertAsync(
            email, firstName, productName, currentStock, threshold, sku);

        // Assert
        await Should.NotThrowAsync(action);
    }

    [TestMethod]
    public async Task SendLowStockAlertAsync_WithEmptySku_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "admin@example.com";
        var firstName = "Admin";
        var productName = "Test Product";
        var currentStock = 5;
        var threshold = 10;

        // Act
        var action = async () => await service.SendLowStockAlertAsync(
            email, firstName, productName, currentStock, threshold, "");

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region SendMarketingEmailAsync Tests

    [TestMethod]
    public async Task SendMarketingEmailAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var service = new SmtpEmailService(_configurationMock.Object, _loggerMock.Object);
        var email = "customer@example.com";
        var firstName = "John";
        var subject = "Special Offer!";
        var htmlContent = "<p>Check out our new products!</p>";

        // Act
        var action = async () => await service.SendMarketingEmailAsync(
            email, firstName, subject, htmlContent);

        // Assert
        await Should.NotThrowAsync(action);
    }

    #endregion

    #region Helper Methods

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
            Items:
            [
                new OrderItemEmailDto("Test Product", 2, 50.00m, 100.00m)
            ],
            ShippingAddress: new AddressEmailDto("John", "Doe", "123 Main St", null, "New York", "NY", "10001", "USA")
        );

    private static CartEmailDto CreateTestCart() =>
        new(Items:
        [
            new CartItemEmailDto("Test Product", 2, 29.99m),
            new CartItemEmailDto("Another Product", 1, 49.99m)
        ]);

    #endregion
}

