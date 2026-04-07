using ECommerce.Core.Enums;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payments.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandHandler(
    IPaymentOrderRepository orderRepository,
    IPaymentStore paymentStore,
    IIdempotencyStore idempotencyStore,
    IConfiguration configuration,
    ILogger<ProcessPaymentCommandHandler> logger)
    : IRequestHandler<ProcessPaymentCommand, Result<PaymentResponseDto>>
{
    private static readonly HashSet<string> SupportedPaymentMethods = new(StringComparer.Ordinal)
    {
        "stripe", "paypal", "credit_card", "debit_card", "apple_pay", "google_pay"
    };

    public async Task<Result<PaymentResponseDto>> Handle(ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.IdempotencyKey, out _))
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.InvalidIdempotencyKey);

        var key = $"payments:process:{command.IdempotencyKey}";
        var start = await idempotencyStore.StartAsync<PaymentResponseDto>(key, TimeSpan.FromMinutes(5), cancellationToken);

        if (start.Status == IdempotencyStartStatus.Replay && start.CachedResponse is not null)
            return Result<PaymentResponseDto>.Ok(start.CachedResponse);

        if (start.Status == IdempotencyStartStatus.InProgress)
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.IdempotencyInProgress);

        var dto = command.Payment;

        var order = await orderRepository.GetByIdAsync(dto.OrderId, trackChanges: true, cancellationToken);
        if (order is null)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.OrderNotFound);
        }

        var method = NormalizePaymentMethod(dto.PaymentMethod);
        if (!SupportedPaymentMethods.Contains(method))
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.UnsupportedPaymentMethod);
        }

        if (dto.Amount != order.TotalAmount)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.PaymentAmountMismatch);
        }

        var paymentIntentId = GenerateMockPaymentIntentId(method);
        var transactionId = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        var paymentSucceeds = !ShouldSimulatePaymentFailure(configuration);

        try
        {
            if (paymentSucceeds)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaymentMethod = dto.PaymentMethod;
                order.PaymentIntentId = paymentIntentId;
                order.Status = OrderStatus.Confirmed;

                await orderRepository.UpdateAsync(order, cancellationToken);

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

                await paymentStore.StorePaymentAsync(paymentIntentId, paymentDetails, cancellationToken);

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
                        { "Provider", GetPaymentProviderName(method) }
                    }
                };

                await idempotencyStore.CompleteAsync(key, response, TimeSpan.FromHours(24), cancellationToken);
                return Result<PaymentResponseDto>.Ok(response);
            }

            order.PaymentStatus = PaymentStatus.Failed;
            order.PaymentIntentId = paymentIntentId;
            await orderRepository.UpdateAsync(order, cancellationToken);

            var failed = new PaymentResponseDto
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                Message = PaymentsApplicationErrors.PaymentDeclined.Message,
                Status = "failed",
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string> { { "OrderNumber", order.OrderNumber } }
            };

            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Ok(failed);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while processing payment for order {OrderId}", dto.OrderId);
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.ConcurrencyConflict);
        }
    }

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        var normalized = paymentMethod.ToLowerInvariant();
        return normalized == "card" ? "credit_card" : normalized;
    }

    private static string GenerateMockPaymentIntentId(string method)
    {
        var prefix = method switch
        {
            "stripe" => "pi_",
            "paypal" => "ppi_",
            "apple_pay" => "ap_",
            "google_pay" => "gp_",
            _ => "pi_"
        };

        return string.Concat(prefix, Guid.NewGuid().ToString("N").AsSpan(0, 20));
    }

    private static string GetPaymentProviderName(string method) => method switch
    {
        "stripe" => "Stripe",
        "paypal" => "PayPal",
        "apple_pay" => "Apple Pay",
        "google_pay" => "Google Pay",
        "credit_card" => "Credit Card",
        "debit_card" => "Debit Card",
        _ => "Unknown"
    };

    private static bool ShouldSimulatePaymentFailure(IConfiguration configuration)
    {
        var simulateFailures = configuration.GetValue<bool>("Payment:SimulateFailures", false);
        if (!simulateFailures)
            return false;

        var random = new Random();
        return random.Next(0, 100) < 5;
    }
}
