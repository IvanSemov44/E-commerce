namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Base class for all request parameter DTOs.
/// Provides common pagination, searching, and sorting functionality.
/// Following CodeMaze best practices for parameter inheritance.
/// </summary>
public abstract class RequestParameters
{
    /// <summary>
    /// Maximum allowed page size to prevent excessive data retrieval.
    /// </summary>
    private const int MaxPageSize = 100;
    
    /// <summary>
    /// Default page size when not specified.
    /// </summary>
    private const int DefaultPageSize = 10;
    
    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// Defaults to 1 if not specified or if value is less than 1.
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value > 0 ? value : 1;
    }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// Constrained between 1 and MaxPageSize (100).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= MaxPageSize ? value : DefaultPageSize;
    }

    /// <summary>
    /// Gets or sets the search term for general text searching.
    /// Applied to searchable fields defined by each entity.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the field name to sort by.
    /// Must match a valid sortable property name.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort order (asc or desc).
    /// Defaults to ascending if not specified.
    /// </summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>
    /// Calculates the number of items to skip for pagination.
    /// </summary>
    /// <returns>Number of items to skip based on current page and page size.</returns>
    public int GetSkip() => (PageNumber - 1) * PageSize;
    
    /// <summary>
    /// Indicates whether sorting should be descending.
    /// </summary>
    public bool IsDescending => SortOrder?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? false;
}
