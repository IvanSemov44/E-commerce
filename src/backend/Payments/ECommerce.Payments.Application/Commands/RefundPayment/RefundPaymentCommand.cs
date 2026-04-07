using ECommerce.Payments.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Commands.RefundPayment;

public record RefundPaymentCommand(Guid OrderId, RefundPaymentDto Refund, string IdempotencyKey)
    : IRequest<Result<RefundResponseDto>>, ITransactionalCommand;
