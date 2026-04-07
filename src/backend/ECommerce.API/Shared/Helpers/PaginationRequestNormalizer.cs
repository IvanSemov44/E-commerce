using ECommerce.SharedKernel.Constants;

namespace ECommerce.API.Shared.Helpers;

/// <summary>
/// Provides shared normalization for pagination query parameters.
/// </summary>
public static class PaginationRequestNormalizer
{
    public static (int Page, int PageSize) Normalize(
        int page,
        int pageSize,
        int defaultPageSize = PaginationConstants.DefaultPageSize)
    {
        var normalizedPage = Math.Max(PaginationConstants.MinPageNumber, page);
        var normalizedPageSize = pageSize < PaginationConstants.MinPageSize
            ? defaultPageSize
            : Math.Min(pageSize, PaginationConstants.MaxPageSize);

        return (normalizedPage, normalizedPageSize);
    }
}

