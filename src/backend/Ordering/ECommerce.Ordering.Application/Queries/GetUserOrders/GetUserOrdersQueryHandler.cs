using ECommerce.Ordering.Application.Mapping;

namespace ECommerce.Ordering.Application.Queries.GetUserOrders;

public class GetUserOrdersQueryHandler(IOrderRepository orders) : IRequestHandler<GetUserOrdersQuery, Result<PaginatedResult<OrderDto>>>
{
    public async Task<Result<PaginatedResult<OrderDto>>> Handle(GetUserOrdersQuery request, CancellationToken ct)
    {
        var items = await orders.GetPagedByUserIdAsync(request.UserId, request.Page, request.PageSize, ct);
        var total = await orders.GetByUserIdCountAsync(request.UserId, ct);
        return Result<PaginatedResult<OrderDto>>.Ok(new PaginatedResult<OrderDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
