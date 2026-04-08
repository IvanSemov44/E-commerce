using ECommerce.Payments.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Queries.GetPaymentIntent;

public record GetPaymentIntentQuery(string PaymentIntentId)
    : IRequest<Result<PaymentDetailsDto>>;
