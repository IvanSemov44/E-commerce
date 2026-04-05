using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;
