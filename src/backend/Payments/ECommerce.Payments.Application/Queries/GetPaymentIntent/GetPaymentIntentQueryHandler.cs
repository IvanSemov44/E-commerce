using ECommerce.Payments.Application.DTOs;
using ECommerce.Payments.Application.Errors;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Queries.GetPaymentIntent;

public sealed class GetPaymentIntentQueryHandler(IPaymentStore paymentStore)
    : IRequestHandler<GetPaymentIntentQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(GetPaymentIntentQuery query, CancellationToken cancellationToken)
    {
        var paymentDetails = await paymentStore.GetPaymentAsync(query.PaymentIntentId, cancellationToken);
        if (paymentDetails is null)
            return Result<PaymentDetailsDto>.Fail(PaymentsApplicationErrors.PaymentIntentNotFound);

        return Result<PaymentDetailsDto>.Ok(paymentDetails);
    }
}
