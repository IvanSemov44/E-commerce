namespace ECommerce.Core.Constants;

/// <summary>
/// Centralized constants for pagination across the application.
/// Ensures consistency and prevents magic numbers in business logic.
/// </summary>
public static class PaginationConstants
{
    /// <summary>
    /// Default page size when not specified. (20 items per page)
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum allowed page size to prevent DoS attacks. (100 items per page)
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Minimum valid page number. (Pages are 1-indexed, never 0)
    /// </summary>
    public const int MinPageNumber = 1;

    /// <summary>
    /// Minimum items per page.
    /// </summary>
    public const int MinPageSize = 1;
}
