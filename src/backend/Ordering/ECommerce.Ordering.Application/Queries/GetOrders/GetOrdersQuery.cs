namespace ECommerce.Ordering.Application.Queries.GetOrders;

public record GetOrdersQuery(int Page, int PageSize) : IRequest<Result<PaginatedResult<OrderDto>>>;
