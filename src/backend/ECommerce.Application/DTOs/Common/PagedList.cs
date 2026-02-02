namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Generic paged list that extends List<T> with pagination metadata.
/// Used to return paginated results with navigation information.
/// Following CodeMaze best practices.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PagedList<T> : List<T>
{
    /// <summary>
    /// Gets the metadata associated with this paged list.
    /// </summary>
    public MetaData MetaData { get; set; }

    /// <summary>
    /// Initializes a new instance of the PagedList class.
    /// </summary>
    /// <param name="items">The items for this page.</param>
    /// <param name="count">The total count of all items.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    public PagedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        MetaData = new MetaData
        {
            TotalCount = count,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
        AddRange(items);
    }

    /// <summary>
    /// Converts an IEnumerable collection to a paged list (synchronous).
    /// Use this when you have already materialized the collection.
    /// </summary>
    /// <param name="source">The source enumerable collection.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A PagedList containing the items for the specified page.</returns>
    public static PagedList<T> ToPagedList(
        IEnumerable<T> source,
        int pageNumber,
        int pageSize)
    {
        var count = source.Count();
        var items = source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
