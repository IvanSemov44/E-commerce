using ECommerce.Payments.Domain.Aggregates.Payment;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payments.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentOrderQuery orderQuery,
    IPaymentGateway paymentGateway,
    IPaymentStore paymentStore,
    IIdempotencyStore idempotencyStore,
    ILogger<ProcessPaymentCommandHandler> logger)
    : IRequestHandler<ProcessPaymentCommand, Result<PaymentResponseDto>>
{
    private static readonly HashSet<string> _supportedMethods = new(StringComparer.Ordinal)
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
        var method = NormalizeMethod(dto.PaymentMethod);

        var order = await orderQuery.GetByOrderIdAsync(dto.OrderId, cancellationToken);
        if (order is null)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.OrderNotFound);
        }

        if (!_supportedMethods.Contains(method))
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.UnsupportedPaymentMethod);
        }

        if (dto.Amount != order.Amount)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.PaymentAmountMismatch);
        }

        try
        {
            var charge = await paymentGateway.ChargeAsync(method, dto.Amount, cancellationToken);
            var payment = Payment.Initiate(dto.OrderId, dto.PaymentMethod, dto.Amount);

            if (charge.Succeeded)
            {
                payment.MarkPaid(charge.PaymentIntentId, charge.TransactionId);
                await paymentRepository.AddAsync(payment, cancellationToken);

                await paymentStore.StorePaymentAsync(charge.PaymentIntentId, new PaymentDetailsDto
                {
                    OrderId = dto.OrderId,
                    PaymentIntentId = charge.PaymentIntentId,
                    Status = "completed",
                    PaymentMethod = dto.PaymentMethod,
                    Amount = dto.Amount,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                }, cancellationToken);

                var response = new PaymentResponseDto
                {
                    Success = true,
                    PaymentIntentId = charge.PaymentIntentId,
                    TransactionId = charge.TransactionId,
                    Message = "Payment processed successfully",
                    Status = "completed",
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string> { { "Provider", charge.ProviderName } }
                };

                await idempotencyStore.CompleteAsync(key, response, TimeSpan.FromHours(24), cancellationToken);
                return Result<PaymentResponseDto>.Ok(response);
            }

            payment.MarkFailed(charge.FailureReason ?? "Payment declined");
            await paymentRepository.AddAsync(payment, cancellationToken);

            var failed = new PaymentResponseDto
            {
                Success = false,
                PaymentIntentId = charge.PaymentIntentId,
                Message = charge.FailureReason ?? PaymentsApplicationErrors.PaymentDeclined.Message,
                Status = "failed",
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ProcessedAt = DateTime.UtcNow
            };

            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Ok(failed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while processing payment for order {OrderId}", dto.OrderId);
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<PaymentResponseDto>.Fail(PaymentsApplicationErrors.InternalError);
        }
    }

    private static string NormalizeMethod(string method)
    {
        var normalized = method.ToLowerInvariant();
        return normalized == "card" ? "credit_card" : normalized;
    }
}
