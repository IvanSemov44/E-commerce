namespace ECommerce.Ordering.Application.Queries.GetOrders;

public record GetOrdersQuery : IRequest<Result<List<OrderDto>>>;
