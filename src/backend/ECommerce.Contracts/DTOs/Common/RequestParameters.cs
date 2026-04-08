using ECommerce.SharedKernel.Constants;

namespace ECommerce.Contracts.DTOs.Common;

/// <summary>
/// Abstract base class for paginated query parameters.
/// Provides shared pagination, search, and sort properties.
/// Derive a class per feature and add only the filter properties that endpoint needs.
/// </summary>
public abstract class RequestParameters
{
    private int _page = 1;
    private int _pageSize = PaginationConstants.DefaultPageSize;

    /// <summary>
    /// Page number (1-based). Defaults to 1. Values less than 1 are clamped to 1.
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 1;
    }

    /// <summary>
    /// Items per page. Clamped between 1 and 100. Defaults to 20.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= PaginationConstants.MaxPageSize ? value : PaginationConstants.DefaultPageSize;
    }

    /// <summary>
    /// Free-text search term. Applied to searchable fields defined by each feature.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Field name to sort by (e.g. "name", "price", "createdAt").
    /// Validated per endpoint via FluentValidation.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" (default) or "desc".
    /// </summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>
    /// Number of rows to skip for the current page. Use this instead of manual (Page-1)*PageSize.
    /// </summary>
    public int GetSkip() => (Page - 1) * PageSize;

    /// <summary>
    /// True when SortOrder is "desc" (case-insensitive).
    /// </summary>
    public bool IsDescending => SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

