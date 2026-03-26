namespace ECommerce.Catalog.Application.DTOs.Common;

public class PaginatedResult<T>
{
    public System.Collections.Generic.IReadOnlyList<T> Items { get; init; } = System.Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
