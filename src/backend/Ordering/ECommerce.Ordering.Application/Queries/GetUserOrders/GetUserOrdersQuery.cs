using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Queries.GetUserOrders;

public record GetUserOrdersQuery(Guid UserId) : IRequest<Result<List<OrderDto>>>;
