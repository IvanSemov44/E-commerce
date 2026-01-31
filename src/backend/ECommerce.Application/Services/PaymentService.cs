using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Mocked payment service for Stripe/PayPal integration.
/// In production, this would call actual payment provider APIs.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;
    private static readonly Dictionary<string, PaymentDetailsDto> MockPaymentStore = new();

    public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        _logger.LogInformation("Processing payment for order {OrderId} via {PaymentMethod}", dto.OrderId, dto.PaymentMethod);

        var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment processing", dto.OrderId);
            throw new OrderNotFoundException(dto.OrderId);
        }

        if (!await IsPaymentMethodSupportedAsync(dto.PaymentMethod))
        {
            _logger.LogWarning("Unsupported payment method: {PaymentMethod}", dto.PaymentMethod);
            throw new UnsupportedPaymentMethodException(dto.PaymentMethod);
        }

        if (dto.Amount != order.TotalAmount)
        {
            _logger.LogWarning("Payment amount {Amount} does not match order total {OrderTotal}", dto.Amount, order.TotalAmount);
            throw new PaymentAmountMismatchException(order.TotalAmount, dto.Amount);
        }

        var paymentIntentId = GenerateMockPaymentIntentId(dto.PaymentMethod);
        var transactionId = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();

        bool paymentSucceeds = !ShouldSimulatePaymentFailure();

        if (paymentSucceeds)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentMethod = dto.PaymentMethod;
            order.PaymentIntentId = paymentIntentId;
            order.Status = OrderStatus.Confirmed;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var paymentDetails = new PaymentDetailsDto
            {
                OrderId = dto.OrderId,
                PaymentIntentId = paymentIntentId,
                Status = "completed",
                PaymentMethod = dto.PaymentMethod,
                Amount = dto.Amount,
                Currency = order.Currency,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            MockPaymentStore[paymentIntentId] = paymentDetails;

            _logger.LogInformation("Payment successful for order {OrderId}. PaymentIntentId: {PaymentIntentId}", dto.OrderId, paymentIntentId);

            return new PaymentResponseDto
            {
                Success = true,
                PaymentIntentId = paymentIntentId,
                TransactionId = transactionId,
                Message = "Payment processed successfully",
                Status = "completed",
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    { "OrderNumber", order.OrderNumber },
                    { "Provider", GetPaymentProviderName(dto.PaymentMethod) }
                }
            };
        }
        else
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.PaymentIntentId = paymentIntentId;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning("Payment failed for order {OrderId}. Simulated failure.", dto.OrderId);

            return new PaymentResponseDto
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                Message = "Payment declined. Please check your payment details and try again.",
                Status = "failed",
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    { "OrderNumber", order.OrderNumber }
                }
            };
        }
    }

    public async Task<PaymentDetailsDto> GetPaymentDetailsAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving payment details for order {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new OrderNotFoundException(orderId);
        }

        if (string.IsNullOrEmpty(order.PaymentIntentId))
        {
            throw new NoPaymentFoundException(orderId);
        }

        if (MockPaymentStore.TryGetValue(order.PaymentIntentId, out var paymentDetails))
        {
            return paymentDetails;
        }

        return new PaymentDetailsDto
        {
            OrderId = orderId,
            PaymentIntentId = order.PaymentIntentId,
            Status = order.PaymentStatus.ToString().ToLower(),
            PaymentMethod = order.PaymentMethod ?? "unknown",
            Amount = order.TotalAmount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            ProcessedAt = order.PaymentStatus == PaymentStatus.Paid ? order.UpdatedAt : null
        };
    }

    public async Task<RefundResponseDto> RefundPaymentAsync(RefundPaymentDto dto)
    {
        _logger.LogInformation("Processing refund for order {OrderId}", dto.OrderId);

        var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
        if (order == null)
        {
            throw new OrderNotFoundException(dto.OrderId);
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new InvalidRefundException($"Cannot refund order with payment status: {order.PaymentStatus}");
        }

        var refundAmount = dto.Amount ?? order.TotalAmount;

        var refundId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();

        order.PaymentStatus = PaymentStatus.Refunded;
        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Refund processed for order {OrderId}. RefundId: {RefundId}", dto.OrderId, refundId);

        return new RefundResponseDto
        {
            Success = true,
            RefundId = refundId,
            Amount = refundAmount,
            Status = "completed",
            Message = "Refund processed successfully",
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<PaymentDetailsDto?> GetPaymentIntentAsync(string paymentIntentId)
    {
        _logger.LogInformation("Retrieving payment intent {PaymentIntentId}", paymentIntentId);

        if (MockPaymentStore.TryGetValue(paymentIntentId, out var paymentDetails))
        {
            return paymentDetails;
        }

        return null;
    }

    public async Task<bool> IsPaymentMethodSupportedAsync(string paymentMethod)
    {
        var supportedMethods = new[] { "stripe", "paypal", "credit_card", "debit_card", "apple_pay", "google_pay" };
        return supportedMethods.Contains(paymentMethod.ToLower());
    }

    private string GenerateMockPaymentIntentId(string paymentMethod)
    {
        var prefix = paymentMethod.ToLower() switch
        {
            "stripe" => "pi_",
            "paypal" => "ppi_",
            "apple_pay" => "ap_",
            "google_pay" => "gp_",
            _ => "pi_"
        };

        return prefix + Guid.NewGuid().ToString("N").Substring(0, 20);
    }

    private string GetPaymentProviderName(string paymentMethod)
    {
        return paymentMethod.ToLower() switch
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

    private bool ShouldSimulatePaymentFailure()
    {
        var random = new Random();
        return random.Next(0, 100) < 5;
    }
}
