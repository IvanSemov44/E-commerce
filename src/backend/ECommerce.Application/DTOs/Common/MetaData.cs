namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Contains pagination metadata for paged responses.
/// Provides navigation information for client pagination controls.
/// </summary>
public class MetaData
{
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total count of all items (across all pages).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;
}
