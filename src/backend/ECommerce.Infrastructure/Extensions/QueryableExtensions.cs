namespace ECommerce.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

/// <summary>
/// Extension methods for IQueryable collections.
/// Provides async paging, filtering, sorting, and searching capabilities.
/// Follows CodeMaze best practices for query composition and separation of concerns.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Gets the total count and items for a paginated query asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing total count and paged items.</returns>
    public static async Task<(int totalCount, List<T> items)> GetPagedDataAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (count, items);
    }

    /// <summary>
    /// Applies a sort order to the query based on sort field and direction.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="sortBy">The property name to sort by.</param>
    /// <param name="ascending">True for ascending order, false for descending.</param>
    /// <returns>The sorted queryable collection.</returns>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string sortBy,
        bool ascending = true)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return source;

        var properties = typeof(T).GetProperties();
        var property = properties.FirstOrDefault(p => 
            string.Equals(p.Name, sortBy, StringComparison.OrdinalIgnoreCase));

        if (property == null)
            return source;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = ascending ? "OrderBy" : "OrderByDescending";
        var orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.PropertyType);

        return (IQueryable<T>)orderByMethod.Invoke(null, new object[] { source, lambda })!;
    }

    /// <summary>
    /// Filters items based on a predicate condition.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered queryable collection.</returns>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>>? predicate)
    {
        return predicate != null ? System.Linq.Queryable.Where(source, predicate) : source;
    }

    /// <summary>
    /// Filters items based on a string property containing a search term.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="property">The property to search in.</param>
    /// <param name="searchTerm">The search term to look for.</param>
    /// <returns>The filtered queryable collection.</returns>
    public static IQueryable<T> SearchBy<T>(
        this IQueryable<T> source,
        Expression<Func<T, string?>> property,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        return source.Where(x => EF.Functions.ILike(
            property.Compile()(x) ?? string.Empty, 
            $"%{searchTerm}%"));
    }

    /// <summary>
    /// Filters numeric items within a range (inclusive).
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TProperty">The numeric property type.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="property">The property to filter.</param>
    /// <param name="minValue">The minimum value (inclusive).</param>
    /// <param name="maxValue">The maximum value (inclusive).</param>
    /// <returns>The filtered queryable collection.</returns>
    public static IQueryable<T> InRange<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> property,
        TProperty minValue,
        TProperty maxValue)
        where TProperty : IComparable<TProperty>
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var member = Expression.Invoke(property, parameter);
        
        var minConst = Expression.Constant(minValue);
        var maxConst = Expression.Constant(maxValue);
        
        var greaterThanOrEqual = Expression.GreaterThanOrEqual(member, minConst);
        var lessThanOrEqual = Expression.LessThanOrEqual(member, maxConst);
        var combined = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        
        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
        return source.Where(lambda);
    }

    /// <summary>
    /// Filters items where a numeric property is greater than a minimum value.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TProperty">The numeric property type.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="property">The property to filter.</param>
    /// <param name="minValue">The minimum value (exclusive).</param>
    /// <returns>The filtered queryable collection.</returns>
    public static IQueryable<T> GreaterThan<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> property,
        TProperty minValue)
        where TProperty : IComparable<TProperty>
    {
        var constant = Expression.Constant(minValue);
        var parameter = Expression.Parameter(typeof(T), "x");
        var memberAccess = Expression.Invoke(property, parameter);
        var comparison = Expression.GreaterThan(memberAccess, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
        return source.Where(lambda);
    }

    /// <summary>
    /// Filters items where a numeric property is less than a maximum value.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <typeparam name="TProperty">The numeric property type.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="property">The property to filter.</param>
    /// <param name="maxValue">The maximum value (exclusive).</param>
    /// <returns>The filtered queryable collection.</returns>
    public static IQueryable<T> LessThan<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> property,
        TProperty maxValue)
        where TProperty : IComparable<TProperty>
    {
        var constant = Expression.Constant(maxValue);
        var parameter = Expression.Parameter(typeof(T), "x");
        var memberAccess = Expression.Invoke(property, parameter);
        var comparison = Expression.LessThan(memberAccess, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
        return source.Where(lambda);
    }

    /// <summary>
    /// Counts matching items asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of items.</returns>
    public static Task<int> CountAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default)
        => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(source, cancellationToken);
}


