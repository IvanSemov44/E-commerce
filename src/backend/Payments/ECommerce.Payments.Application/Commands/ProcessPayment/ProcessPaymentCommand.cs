using ECommerce.Payments.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Payments.Application.Commands.ProcessPayment;

public record ProcessPaymentCommand(ProcessPaymentDto Payment, string IdempotencyKey)
    : IRequest<Result<PaymentResponseDto>>, ITransactionalCommand;
