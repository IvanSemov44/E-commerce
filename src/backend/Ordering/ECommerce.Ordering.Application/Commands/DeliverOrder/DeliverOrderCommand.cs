using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Commands.DeliverOrder;

public record DeliverOrderCommand(Guid OrderId) : IRequest<Result<OrderDto>>;
