using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class PaymentServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IOrderRepository> _mockOrderRepository = null!;
    private Mock<ILogger<PaymentService>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IPaymentStore> _mockPaymentStore = null!;
    private PaymentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<PaymentService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockPaymentStore = new Mock<IPaymentStore>();

        _mockUnitOfWork.Setup(u => u.Orders).Returns(_mockOrderRepository.Object);

        // Setup configuration defaults
        _mockConfiguration.Setup(c => c["Payment:SimulateFailures"]).Returns("false");
        _mockConfiguration.Setup(c => c.GetSection("Payment:SimulateFailures").Value).Returns("false");

        _service = new PaymentService(
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockPaymentStore.Object);
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.Amount.Should().Be(100m);
            success.Data.PaymentMethod.Should().Be("stripe");
            success.Data.PaymentIntentId.Should().NotBeNullOrEmpty();
            success.Data.Status.Should().Be("completed");
            success.Data.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            success.Data.Metadata.Should().NotBeNull();
            success.Data.TransactionId.Should().NotBeNullOrEmpty();
        }
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_OrderNotFound_ReturnsFailureResult()
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

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<PaymentResponseDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.OrderNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<PaymentResponseDto>.Failure");
        }
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_UnsupportedPaymentMethod_ReturnsFailureResult()
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

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<PaymentResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("UNSUPPORTED_PAYMENT_METHOD");
        }
        else
        {
            Assert.Fail("Expected Result<PaymentResponseDto>.Failure");
        }
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_AmountMismatch_ReturnsFailureResult()
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

        // Act
        var result = await _service.ProcessPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<PaymentResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("PAYMENT_AMOUNT_MISMATCH");
        }
        else
        {
            Assert.Fail("Expected Result<PaymentResponseDto>.Failure");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.PaymentIntentId.Should().StartWith("pi_");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.PaymentIntentId.Should().StartWith("ppi_");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.PaymentIntentId.Should().StartWith("ap_");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.PaymentIntentId.Should().StartWith("gp_");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            order.PaymentStatus.Should().Be(PaymentStatus.Paid);
            order.Status.Should().Be(OrderStatus.Confirmed);
            order.PaymentMethod.Should().Be("stripe");
            order.PaymentIntentId.Should().NotBeNullOrEmpty();
            success.Data.Status.Should().Be("completed");
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentResponseDto>.Success success)
        {
            success.Data.Metadata.Should().NotBeNull();
            success.Data.Metadata.Should().ContainKey("OrderNumber");
            success.Data.Metadata!["OrderNumber"].Should().Be("ORD-12345");
            success.Data.Metadata.Should().ContainKey("Provider");
            success.Data.Metadata["Provider"].Should().Be("Stripe");
            success.Data.Status.Should().Be("completed");
        }
    }

    #endregion

    #region GetPaymentDetailsAsync Tests

    [TestMethod]
    public async Task GetPaymentDetailsAsync_OrderNotFound_ReturnsFailureResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetPaymentDetailsAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<PaymentDetailsDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.OrderNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<PaymentDetailsDto>.Failure");
        }
    }

    [TestMethod]
    public async Task GetPaymentDetailsAsync_NoPaymentIntentId_ReturnsFailureResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = TestDataFactory.CreateOrder(userId);
        var orderId = order.Id;
        order.PaymentIntentId = null;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetPaymentDetailsAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<PaymentDetailsDto>.Failure failure)
        {
            failure.Code.Should().Be("NO_PAYMENT_FOUND");
        }
        else
        {
            Assert.Fail("Expected Result<PaymentDetailsDto>.Failure");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentDetailsDto>.Success success)
        {
            success.Data.OrderId.Should().Be(orderId);
            success.Data.PaymentIntentId.Should().Be("pi_test123");
            success.Data.PaymentMethod.Should().Be("stripe");
            success.Data.Amount.Should().Be(100m);
            success.Data.Status.Should().Be("paid");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentDetailsDto>.Success success)
        {
            success.Data.ProcessedAt.Should().NotBeNull();
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<PaymentDetailsDto>.Success success)
        {
            success.Data.ProcessedAt.Should().BeNull();
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<RefundResponseDto>.Success success)
        {
            success.Data.RefundId.Should().NotBeNullOrEmpty();
            success.Data.Amount.Should().Be(100m);
            success.Data.Status.Should().Be("completed");
            success.Data.Message.Should().Be("Refund processed successfully");
            success.Data.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public async Task RefundPaymentAsync_OrderNotFound_ReturnsFailureResult()
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

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<RefundResponseDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.OrderNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<RefundResponseDto>.Failure");
        }
    }

    [TestMethod]
    public async Task RefundPaymentAsync_UnpaidOrder_ReturnsFailureResult()
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

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<RefundResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_REFUND");
            failure.Message.Should().Contain("Cannot refund order with payment status");
        }
        else
        {
            Assert.Fail("Expected Result<RefundResponseDto>.Failure");
        }
    }

    [TestMethod]
    public async Task RefundPaymentAsync_FailedPayment_ReturnsFailureResult()
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

        // Act
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<RefundResponseDto>.Failure failure)
        {
            failure.Code.Should().Be("INVALID_REFUND");
            failure.Message.Should().Contain("Cannot refund order with payment status");
        }
        else
        {
            Assert.Fail("Expected Result<RefundResponseDto>.Failure");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<RefundResponseDto>.Success success)
        {
            success.Data.Amount.Should().Be(150m);
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<RefundResponseDto>.Success success)
        {
            success.Data.Amount.Should().Be(50m);
        }
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
        var result = await _service.RefundPaymentAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
