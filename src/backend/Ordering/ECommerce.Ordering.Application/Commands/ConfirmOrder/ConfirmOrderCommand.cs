using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<Result<OrderDto>>;
