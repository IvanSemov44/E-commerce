using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Payments.Domain.Enums;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Queries.GetPaymentDetails;

public sealed class GetPaymentDetailsQueryHandler(
    IPaymentRepository paymentRepository,
    IPaymentOrderQuery orderQuery,
    IPaymentStore paymentStore)
    : IRequestHandler<GetPaymentDetailsQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(GetPaymentDetailsQuery query, CancellationToken cancellationToken)
    {
        var order = await orderQuery.GetByOrderIdAsync(query.OrderId, cancellationToken);
        if (order is null)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.OrderNotFound);

        if (!query.IsAdmin && order.UserId != query.UserId)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.Forbidden);

        var payment = await paymentRepository.GetByOrderIdAsync(query.OrderId, cancellationToken);
        if (payment is null)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.NoPaymentFound);

        if (!string.IsNullOrEmpty(payment.PaymentIntentId))
        {
            var cached = await paymentStore.GetPaymentAsync(payment.PaymentIntentId, cancellationToken);
            if (cached is not null)
                return Result<PaymentDetailsDto>.Ok(cached);
        }

        var details = new PaymentDetailsDto
        {
            OrderId = query.OrderId,
            PaymentIntentId = payment.PaymentIntentId,
            Status = payment.Status.ToString().ToLowerInvariant(),
            PaymentMethod = payment.PaymentMethod,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.Status == PaymentStatus.Paid ? payment.ProcessedAt : null
        };

        return Result<PaymentDetailsDto>.Ok(details);
    }
}
