using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class PaymentServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IOrderRepository> _mockOrderRepository = null!;
    private Mock<ILogger<PaymentService>> _mockLogger = null!;
    private PaymentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _mockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepository.Object);

        _service = new PaymentService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    #region ProcessPaymentAsync Tests

    [TestMethod]
    public async Task ProcessPaymentAsync_ValidPayment_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m, paymentStatus: PaymentStatus.Pending);
        var orderId = order.Id;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 100m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(100m);
        result.PaymentMethod.Should().Be("stripe");
        result.PaymentIntentId.Should().NotBeNullOrEmpty();

        // Note: Result may be success or failure due to random simulation (5% failure rate)
        // Both are valid outcomes, so we verify based on the result
        result.Status.Should().BeOneOf("completed", "failed");
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Metadata.Should().NotBeNull();

        if (result.Success)
        {
            result.TransactionId.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("completed");
        }
        else
        {
            result.Status.Should().Be("failed");
        }
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_OrderNotFound_ThrowsOrderNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 100m
        };

        // Act & Assert
        await _service.Invoking(s => s.ProcessPaymentAsync(dto))
            .Should().ThrowAsync<OrderNotFoundException>();
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_UnsupportedPaymentMethod_ThrowsUnsupportedPaymentMethodException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "bitcoin", // Unsupported
            Amount = 100m
        };

        // Act & Assert
        await _service.Invoking(s => s.ProcessPaymentAsync(dto))
            .Should().ThrowAsync<UnsupportedPaymentMethodException>();
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_AmountMismatch_ThrowsPaymentAmountMismatchException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 200m // Mismatched amount
        };

        // Act & Assert
        await _service.Invoking(s => s.ProcessPaymentAsync(dto))
            .Should().ThrowAsync<PaymentAmountMismatchException>();
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_StripePaymentMethod_GeneratesCorrectPaymentIntentPrefix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 100m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.PaymentIntentId.Should().StartWith("pi_");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_PayPalPaymentMethod_GeneratesCorrectPaymentIntentPrefix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "paypal",
            Amount = 100m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.PaymentIntentId.Should().StartWith("ppi_");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_ApplePayPaymentMethod_GeneratesCorrectPaymentIntentPrefix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 150m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "apple_pay",
            Amount = 150m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.PaymentIntentId.Should().StartWith("ap_");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_GooglePayPaymentMethod_GeneratesCorrectPaymentIntentPrefix()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 75m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "google_pay",
            Amount = 75m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.PaymentIntentId.Should().StartWith("gp_");
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_PaymentSuccess_UpdatesOrderStatusCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;
        order.Status = OrderStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 100m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert - Verify order is updated based on payment result
        // Note: Payment result is random (5% failure rate) due to simulation
        result.Should().NotBeNull();

        if (result.Success)
        {
            order.PaymentStatus.Should().Be(PaymentStatus.Paid);
            order.Status.Should().Be(OrderStatus.Confirmed);
            order.PaymentMethod.Should().Be("stripe");
            order.PaymentIntentId.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("completed");
        }
        else
        {
            order.PaymentStatus.Should().Be(PaymentStatus.Failed);
            order.PaymentIntentId.Should().NotBeNullOrEmpty();
            result.Status.Should().Be("failed");
        }

        _mockOrderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_MetadataContainsOrderNumberAndProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.OrderNumber = "ORD-12345";

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new ProcessPaymentDto
        {
            OrderId = orderId,
            PaymentMethod = "stripe",
            Amount = 100m
        };

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        // Note: Payment result is random (5% failure rate) due to simulation
        result.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().ContainKey("OrderNumber");
        result.Metadata!["OrderNumber"].Should().Be("ORD-12345");

        // Provider metadata is only included on success
        if (result.Success)
        {
            result.Metadata.Should().ContainKey("Provider");
            result.Metadata["Provider"].Should().Be("Stripe");
            result.Status.Should().Be("completed");
        }
        else
        {
            result.Status.Should().Be("failed");
        }
    }

    #endregion

    #region GetPaymentDetailsAsync Tests

    [TestMethod]
    public async Task GetPaymentDetailsAsync_OrderNotFound_ThrowsOrderNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await _service.Invoking(s => s.GetPaymentDetailsAsync(orderId))
            .Should().ThrowAsync<OrderNotFoundException>();
    }

    [TestMethod]
    public async Task GetPaymentDetailsAsync_NoPaymentIntentId_ThrowsNoPaymentFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentIntentId = null;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act & Assert
        await _service.Invoking(s => s.GetPaymentDetailsAsync(orderId))
            .Should().ThrowAsync<NoPaymentFoundException>();
    }

    [TestMethod]
    public async Task GetPaymentDetailsAsync_WithPaymentIntentId_ReturnsPaymentDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentIntentId = "pi_test123";
        order.PaymentStatus = PaymentStatus.Paid;
        order.PaymentMethod = "stripe";

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetPaymentDetailsAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.PaymentIntentId.Should().Be("pi_test123");
        result.PaymentMethod.Should().Be("stripe");
        result.Amount.Should().Be(100m);
        result.Status.Should().Be("paid");
    }

    [TestMethod]
    public async Task GetPaymentDetailsAsync_ProcessedPayment_ReturnsProcessedAtDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentIntentId = "pi_test123";
        order.PaymentStatus = PaymentStatus.Paid;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetPaymentDetailsAsync(orderId);

        // Assert
        result.ProcessedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetPaymentDetailsAsync_PendingPayment_ProcessedAtIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentIntentId = "pi_test123";
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetPaymentDetailsAsync(orderId);

        // Assert
        result.ProcessedAt.Should().BeNull();
    }

    #endregion

    #region RefundPaymentAsync Tests

    [TestMethod]
    public async Task RefundPaymentAsync_ValidRefund_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Paid;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 100m,
            Reason = "Customer request"
        };

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RefundId.Should().NotBeNullOrEmpty();
        result.Amount.Should().Be(100m);
        result.Status.Should().Be("completed");
        result.Message.Should().Be("Refund processed successfully");
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task RefundPaymentAsync_OrderNotFound_ThrowsOrderNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 100m
        };

        // Act & Assert
        await _service.Invoking(s => s.RefundPaymentAsync(dto))
            .Should().ThrowAsync<OrderNotFoundException>();
    }

    [TestMethod]
    public async Task RefundPaymentAsync_UnpaidOrder_ThrowsInvalidRefundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Pending;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 100m
        };

        // Act & Assert
        await _service.Invoking(s => s.RefundPaymentAsync(dto))
            .Should().ThrowAsync<InvalidRefundException>()
            .WithMessage("*Cannot refund order with payment status: Pending*");
    }

    [TestMethod]
    public async Task RefundPaymentAsync_FailedPayment_ThrowsInvalidRefundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Failed;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 50m
        };

        // Act & Assert
        await _service.Invoking(s => s.RefundPaymentAsync(dto))
            .Should().ThrowAsync<InvalidRefundException>()
            .WithMessage("*Cannot refund order with payment status: Failed*");
    }

    [TestMethod]
    public async Task RefundPaymentAsync_NullAmount_RefundsFullOrderAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 150m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Paid;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = null // Full refund
        };

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.Amount.Should().Be(150m);
    }

    [TestMethod]
    public async Task RefundPaymentAsync_PartialAmount_RefundsSpecifiedAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 150m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Paid;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 50m // Partial refund
        };

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.Amount.Should().Be(50m);
    }

    [TestMethod]
    public async Task RefundPaymentAsync_UpdatesOrderPaymentStatusToRefunded()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId, totalAmount: 100m);
        var orderId = order.Id;
        order.PaymentStatus = PaymentStatus.Paid;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new RefundPaymentDto
        {
            OrderId = orderId,
            Amount = 100m
        };

        // Act
        await _service.RefundPaymentAsync(dto);

        // Assert
        order.PaymentStatus.Should().Be(PaymentStatus.Refunded);
        _mockOrderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPaymentIntentAsync Tests

    [TestMethod]
    public async Task GetPaymentIntentAsync_NonExistentIntent_ReturnsNull()
    {
        // Act
        var result = await _service.GetPaymentIntentAsync("pi_nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsPaymentMethodSupportedAsync Tests

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_Stripe_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("stripe");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_PayPal_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("paypal");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_CreditCard_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("credit_card");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_DebitCard_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("debit_card");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_ApplePay_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("apple_pay");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_GooglePay_ReturnsTrue()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("google_pay");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_Bitcoin_ReturnsFalse()
    {
        // Act
        var result = await _service.IsPaymentMethodSupportedAsync("bitcoin");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPaymentMethodSupportedAsync_CaseInsensitive_ReturnsTrue()
    {
        // Act
        var result1 = await _service.IsPaymentMethodSupportedAsync("STRIPE");
        var result2 = await _service.IsPaymentMethodSupportedAsync("PayPal");
        var result3 = await _service.IsPaymentMethodSupportedAsync("Apple_Pay");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    #endregion
}
