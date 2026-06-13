using ECommerce.Ordering.Application.Mapping;

namespace ECommerce.Ordering.Application.Queries.GetOrders;

public class GetOrdersQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrdersQuery, Result<PaginatedResult<OrderDto>>>
{
    public async Task<Result<PaginatedResult<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var items = await orders.GetPagedAsync(request.Page, request.PageSize, ct);
        var total = await orders.GetTotalOrdersCountAsync(ct);
        return Result<PaginatedResult<OrderDto>>.Ok(new PaginatedResult<OrderDto>
        {
            Items = items.Select(o => o.ToDto()).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
