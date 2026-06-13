namespace ECommerce.Ordering.Application.Queries.GetUserOrders;

public record GetUserOrdersQuery(Guid UserId, int Page, int PageSize) : IRequest<Result<PaginatedResult<OrderDto>>>;
