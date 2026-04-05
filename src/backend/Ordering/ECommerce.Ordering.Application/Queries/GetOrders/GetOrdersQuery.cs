using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Queries.GetOrders;

public record GetOrdersQuery : IRequest<Result<List<OrderDto>>>;
