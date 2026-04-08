using ECommerce.Payments.Core.Enums;
using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Queries.GetPaymentDetails;

public sealed class GetPaymentDetailsQueryHandler(
    IPaymentOrderRepository orderRepository,
    IPaymentStore paymentStore)
    : IRequestHandler<GetPaymentDetailsQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(GetPaymentDetailsQuery query, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, trackChanges: false, cancellationToken);
        if (order is null)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.OrderNotFound);

        if (!query.IsAdmin && order.UserId != query.UserId)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.Forbidden);

        if (string.IsNullOrEmpty(order.PaymentIntentId))
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.NoPaymentFound);

        var paymentDetails = await paymentStore.GetPaymentAsync(order.PaymentIntentId, cancellationToken);
        if (paymentDetails is not null)
            return Result<PaymentDetailsDto>.Ok(paymentDetails);

        var details = new PaymentDetailsDto
        {
            OrderId = query.OrderId,
            PaymentIntentId = order.PaymentIntentId,
            Status = order.PaymentStatus.ToString().ToLowerInvariant(),
            PaymentMethod = order.PaymentMethod ?? "unknown",
            Amount = order.TotalAmount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            ProcessedAt = order.PaymentStatus == (ECommerce.SharedKernel.Enums.PaymentStatus)(int)PaymentStatus.Paid ? order.UpdatedAt : null
        };

        return Result<PaymentDetailsDto>.Ok(details);
    }
}
