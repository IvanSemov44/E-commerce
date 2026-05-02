using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Domain.Enums;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payments.Application.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IIdempotencyStore idempotencyStore,
    ILogger<RefundPaymentCommandHandler> logger)
    : IRequestHandler<RefundPaymentCommand, Result<RefundResponseDto>>
{
    public async Task<Result<RefundResponseDto>> Handle(RefundPaymentCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.IdempotencyKey, out _))
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.InvalidIdempotencyKey);

        var key = $"payments:refund:{command.OrderId}:{command.IdempotencyKey}";
        var start = await idempotencyStore.StartAsync<RefundResponseDto>(key, TimeSpan.FromMinutes(5), cancellationToken);

        if (start.Status == IdempotencyStartStatus.Replay && start.CachedResponse is not null)
            return Result<RefundResponseDto>.Ok(start.CachedResponse);

        if (start.Status == IdempotencyStartStatus.InProgress)
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.IdempotencyInProgress);

        var payment = await paymentRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (payment is null)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.OrderNotFound);
        }

        if (payment.Status != PaymentStatus.Paid)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.InvalidRefund);
        }

        var refundResult = payment.Refund();
        if (!refundResult.IsSuccess)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(refundResult.GetErrorOrThrow());
        }

        var refundAmount = command.Refund.Amount ?? payment.Amount;
        var refundId = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

        try
        {
            var response = new RefundResponseDto
            {
                Success = true,
                RefundId = refundId,
                Amount = refundAmount,
                Status = "completed",
                Message = "Refund processed successfully",
                ProcessedAt = DateTime.UtcNow
            };

            await idempotencyStore.CompleteAsync(key, response, TimeSpan.FromHours(24), cancellationToken);
            return Result<RefundResponseDto>.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while refunding payment for order {OrderId}", command.OrderId);
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.InternalError);
        }
    }
}
