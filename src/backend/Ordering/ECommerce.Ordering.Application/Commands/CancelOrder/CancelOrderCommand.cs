using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<OrderDto>>;
