using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Core.Enums;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Mocked payment service for Stripe/PayPal integration.
/// In production, this would call actual payment provider APIs.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentService> _logger;
    private static readonly Dictionary<string, PaymentDetailsDto> MockPaymentStore = new();

    public PaymentService(IOrderRepository orderRepository, ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        _logger.LogInformation("Processing payment for order {OrderId} via {PaymentMethod}", dto.OrderId, dto.PaymentMethod);

        try
        {
            // Validate order exists
            var order = await _orderRepository.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for payment processing", dto.OrderId);
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = "Order not found",
                    Status = "failed",
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Validate payment method
            if (!await IsPaymentMethodSupportedAsync(dto.PaymentMethod))
            {
                _logger.LogWarning("Unsupported payment method: {PaymentMethod}", dto.PaymentMethod);
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Payment method '{dto.PaymentMethod}' is not supported",
                    Status = "failed",
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Validate amount matches order total
            if (dto.Amount != order.TotalAmount)
            {
                _logger.LogWarning("Payment amount {Amount} does not match order total {OrderTotal}", dto.Amount, order.TotalAmount);
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Payment amount does not match order total. Expected: {order.TotalAmount}, Got: {dto.Amount}",
                    Status = "failed",
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Mock payment processing
            var paymentIntentId = GenerateMockPaymentIntentId(dto.PaymentMethod);
            var transactionId = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();

            // Simulate occasional payment failures (5% failure rate for demo)
            bool paymentSucceeds = !ShouldSimulatePaymentFailure();

            if (paymentSucceeds)
            {
                // Update order with payment information
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaymentMethod = dto.PaymentMethod;
                order.PaymentIntentId = paymentIntentId;
                order.Status = OrderStatus.Confirmed;

                await _orderRepository.UpdateAsync(order);
                await _orderRepository.SaveChangesAsync();

                // Store payment details in mock store
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
                // Mark payment as failed
                order.PaymentStatus = PaymentStatus.Failed;
                order.PaymentIntentId = paymentIntentId;

                await _orderRepository.UpdateAsync(order);
                await _orderRepository.SaveChangesAsync();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", dto.OrderId);
            return new PaymentResponseDto
            {
                Success = false,
                Message = "An error occurred while processing your payment",
                Status = "failed",
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentDetailsDto> GetPaymentDetailsAsync(Guid orderId)
    {
        _logger.LogInformation("Retrieving payment details for order {OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException($"Order {orderId} not found");
        }

        if (string.IsNullOrEmpty(order.PaymentIntentId))
        {
            throw new InvalidOperationException($"No payment intent found for order {orderId}");
        }

        // Check mock store first
        if (MockPaymentStore.TryGetValue(order.PaymentIntentId, out var paymentDetails))
        {
            return paymentDetails;
        }

        // Return order's payment status
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

        try
        {
            var order = await _orderRepository.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                return new RefundResponseDto
                {
                    Success = false,
                    Message = "Order not found",
                    Status = "failed",
                    Amount = dto.Amount ?? 0,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                return new RefundResponseDto
                {
                    Success = false,
                    Message = $"Cannot refund order with payment status: {order.PaymentStatus}",
                    Status = "failed",
                    Amount = dto.Amount ?? 0,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            var refundAmount = dto.Amount ?? order.TotalAmount;

            // Mock refund processing
            var refundId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();

            order.PaymentStatus = PaymentStatus.Refunded;
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId}", dto.OrderId);
            return new RefundResponseDto
            {
                Success = false,
                Message = "An error occurred while processing the refund",
                Status = "failed",
                Amount = dto.Amount ?? 0,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentDetailsDto?> GetPaymentIntentAsync(string paymentIntentId)
    {
        _logger.LogInformation("Retrieving payment intent {PaymentIntentId}", paymentIntentId);

        if (MockPaymentStore.TryGetValue(paymentIntentId, out var paymentDetails))
        {
            return paymentDetails;
        }

        // Payment intent not found in mock store
        return null;
    }

    public async Task<bool> IsPaymentMethodSupportedAsync(string paymentMethod)
    {
        var supportedMethods = new[] { "stripe", "paypal", "credit_card", "debit_card", "apple_pay", "google_pay" };
        return supportedMethods.Contains(paymentMethod.ToLower());
    }

    /// <summary>
    /// Generate a mock payment intent ID based on payment method.
    /// </summary>
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

    /// <summary>
    /// Get human-readable payment provider name.
    /// </summary>
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

    /// <summary>
    /// Simulate occasional payment failures (5% failure rate).
    /// </summary>
    private bool ShouldSimulatePaymentFailure()
    {
        // Use a simple random check for demo purposes
        var random = new Random();
        return random.Next(0, 100) < 5; // 5% failure rate
    }
}
