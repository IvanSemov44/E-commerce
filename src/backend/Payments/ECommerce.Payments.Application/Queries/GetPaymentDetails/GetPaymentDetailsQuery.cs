using ECommerce.Payments.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Queries.GetPaymentDetails;

public record GetPaymentDetailsQuery(Guid OrderId, Guid? UserId, bool IsAdmin)
    : IRequest<Result<PaymentDetailsDto>>;
