namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Base class for all request parameter DTOs.
/// Provides common pagination, searching, and sorting functionality.
/// </summary>
public abstract class RequestParameters
{
    private int _page = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 1;
    }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= 100 ? value : 10;
    }

    /// <summary>
    /// Gets or sets the search term for general searching.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the field name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort order (asc or desc).
    /// Defaults to 'asc' if not specified.
    /// </summary>
    public string? SortOrder { get; set; } = "asc";

    /// <summary>
    /// Calculates the number of items to skip for pagination.
    /// </summary>
    public int GetSkip() => (Page - 1) * PageSize;
}
