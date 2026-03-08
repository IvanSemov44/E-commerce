using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

/// <summary>
/// Mocked payment service for Stripe/PayPal integration.
/// In production, this would call actual payment provider APIs.
/// </summary>
public class PaymentService : IPaymentService
{
    private static readonly HashSet<string> SupportedPaymentMethods = new(StringComparer.Ordinal)
    {
        "stripe", "paypal", "credit_card", "debit_card", "apple_pay", "google_pay"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPaymentStore _paymentStore;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger,
        IConfiguration configuration,
        IPaymentStore paymentStore)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _paymentStore = paymentStore;
    }

    public async Task<Result<PaymentResponseDto>> ProcessPaymentAsync(ProcessPaymentDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment for order {OrderId} via {PaymentMethod}", dto.OrderId, dto.PaymentMethod);

        var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId, cancellationToken: cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment processing", dto.OrderId);
            return Result<PaymentResponseDto>.Fail(ErrorCodes.OrderNotFound, $"Order {dto.OrderId} not found");
        }

        if (!await IsPaymentMethodSupportedAsync(dto.PaymentMethod, cancellationToken))
        {
            _logger.LogWarning("Unsupported payment method: {PaymentMethod}", dto.PaymentMethod);
            return Result<PaymentResponseDto>.Fail(ErrorCodes.UnsupportedPaymentMethod, $"Payment method '{dto.PaymentMethod}' is not supported");
        }

        if (dto.Amount != order.TotalAmount)
        {
            _logger.LogWarning("Payment amount {Amount} does not match order total {OrderTotal}", dto.Amount, order.TotalAmount);
            return Result<PaymentResponseDto>.Fail(ErrorCodes.PaymentAmountMismatch, "Payment amount does not match order total");
        }

        var paymentIntentId = GenerateMockPaymentIntentId(dto.PaymentMethod);
        var transactionId = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();

        bool paymentSucceeds = !ShouldSimulatePaymentFailure();

        try
        {
            if (paymentSucceeds)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaymentMethod = dto.PaymentMethod;
                order.PaymentIntentId = paymentIntentId;
                order.Status = OrderStatus.Confirmed;

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

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

                await _paymentStore.StorePaymentAsync(paymentIntentId, paymentDetails, cancellationToken);

                _logger.LogInformation("Payment successful for order {OrderId}. PaymentIntentId: {PaymentIntentId}", dto.OrderId, paymentIntentId);

                var response = new PaymentResponseDto
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
                return Result<PaymentResponseDto>.Ok(response);
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.PaymentIntentId = paymentIntentId;

                await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                _logger.LogWarning("Payment failed for order {OrderId}. Simulated failure.", dto.OrderId);

                var response = new PaymentResponseDto
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
                return Result<PaymentResponseDto>.Ok(response);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while processing payment for order {OrderId}", dto.OrderId);
            return Result<PaymentResponseDto>.Fail(ErrorCodes.ConcurrencyConflict, "Payment update conflicted with another request. Please retry.");
        }
    }

    public Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
        => GetPaymentDetailsAsync(orderId, userId: null, isAdmin: true, cancellationToken);

    public async Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(Guid orderId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving payment details for order {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, trackChanges: false, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<PaymentDetailsDto>.Fail(ErrorCodes.OrderNotFound, $"Order {orderId} not found");
        }

        if (!isAdmin && order.UserId != userId)
        {
            return Result<PaymentDetailsDto>.Fail(ErrorCodes.Forbidden, "You do not have permission to view payment details for this order");
        }

        if (string.IsNullOrEmpty(order.PaymentIntentId))
        {
            return Result<PaymentDetailsDto>.Fail(ErrorCodes.NoPaymentFound, $"No payment found for order {orderId}");
        }

        var paymentDetails = await _paymentStore.GetPaymentAsync(order.PaymentIntentId, cancellationToken);
        if (paymentDetails != null)
        {
            return Result<PaymentDetailsDto>.Ok(paymentDetails);
        }

        var details = new PaymentDetailsDto
        {
            OrderId = orderId,
            PaymentIntentId = order.PaymentIntentId,
            Status = order.PaymentStatus.ToString().ToLowerInvariant(),
            PaymentMethod = order.PaymentMethod ?? "unknown",
            Amount = order.TotalAmount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            ProcessedAt = order.PaymentStatus == PaymentStatus.Paid ? order.UpdatedAt : null
        };
        return Result<PaymentDetailsDto>.Ok(details);
    }

    public async Task<Result<RefundResponseDto>> RefundPaymentAsync(RefundPaymentDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing refund for order {OrderId}", dto.OrderId);

        var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<RefundResponseDto>.Fail(ErrorCodes.OrderNotFound, $"Order {dto.OrderId} not found");
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            return Result<RefundResponseDto>.Fail(ErrorCodes.InvalidRefund, $"Cannot refund order with payment status: {order.PaymentStatus}");
        }

        var refundAmount = dto.Amount ?? order.TotalAmount;

        var refundId = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

        try
        {
            order.PaymentStatus = PaymentStatus.Refunded;
            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while refunding payment for order {OrderId}", dto.OrderId);
            return Result<RefundResponseDto>.Fail(ErrorCodes.ConcurrencyConflict, "Refund update conflicted with another request. Please retry.");
        }

        _logger.LogInformation("Refund processed for order {OrderId}. RefundId: {RefundId}", dto.OrderId, refundId);

        var response = new RefundResponseDto
        {
            Success = true,
            RefundId = refundId,
            Amount = refundAmount,
            Status = "completed",
            Message = "Refund processed successfully",
            ProcessedAt = DateTime.UtcNow
        };
        return Result<RefundResponseDto>.Ok(response);
    }

    public async Task<Result<PaymentDetailsDto>> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving payment intent {PaymentIntentId}", paymentIntentId);

        var paymentDetails = await _paymentStore.GetPaymentAsync(paymentIntentId, cancellationToken);
        if (paymentDetails == null)
        {
            return Result<PaymentDetailsDto>.Fail(ErrorCodes.PaymentIntentNotFound, "Payment intent not found");
        }

        return Result<PaymentDetailsDto>.Ok(paymentDetails);
    }

    public async Task<bool> IsPaymentMethodSupportedAsync(string paymentMethod, CancellationToken cancellationToken = default)
    {
        var normalizedMethod = NormalizePaymentMethod(paymentMethod);
        return SupportedPaymentMethods.Contains(normalizedMethod);
    }

    private static string GenerateMockPaymentIntentId(string paymentMethod)
    {
        var normalizedMethod = NormalizePaymentMethod(paymentMethod);
        var prefix = normalizedMethod switch
        {
            "stripe" => "pi_",
            "paypal" => "ppi_",
            "apple_pay" => "ap_",
            "google_pay" => "gp_",
            _ => "pi_"
        };

        return string.Concat(prefix, Guid.NewGuid().ToString("N").AsSpan(0, 20));
    }

    private static string GetPaymentProviderName(string paymentMethod)
    {
        var normalizedMethod = NormalizePaymentMethod(paymentMethod);
        return normalizedMethod switch
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

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        var normalizedMethod = paymentMethod.ToLowerInvariant();
        return normalizedMethod == "card" ? "credit_card" : normalizedMethod;
    }

    private bool ShouldSimulatePaymentFailure()
    {
        // Only simulate failures in development/testing when explicitly enabled
        var simulateFailures = _configuration.GetValue<bool>("Payment:SimulateFailures", false);

        if (!simulateFailures)
            return false;

        var random = new Random();
        return random.Next(0, 100) < 5;
    }
}
