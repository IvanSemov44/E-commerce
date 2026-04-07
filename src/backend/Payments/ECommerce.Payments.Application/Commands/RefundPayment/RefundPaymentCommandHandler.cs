using ECommerce.Core.Enums;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payments.Application.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentOrderRepository orderRepository,
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

        var order = await orderRepository.GetByIdAsync(command.OrderId, trackChanges: true, cancellationToken);
        if (order is null)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.OrderNotFound);
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.InvalidRefund);
        }

        var refundAmount = command.Refund.Amount ?? order.TotalAmount;
        var refundId = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();

        try
        {
            order.PaymentStatus = PaymentStatus.Refunded;
            await orderRepository.UpdateAsync(order, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while refunding payment for order {OrderId}", command.OrderId);
            await idempotencyStore.AbandonAsync(key, cancellationToken);
            return Result<RefundResponseDto>.Fail(PaymentsApplicationErrors.ConcurrencyConflict);
        }
    }
}
