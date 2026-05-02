namespace ECommerce.Ordering.Application.Queries.GetUserOrders;

public record GetUserOrdersQuery(Guid UserId) : IRequest<Result<List<OrderDto>>>;
