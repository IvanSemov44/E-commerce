namespace ECommerce.Tests.Helpers;

/// <summary>
/// Extension methods for creating testable async queryables.
/// </summary>
public static class AsyncQueryableExtensions
{
    /// <summary>
    /// Converts a List to an IQueryable that supports async LINQ operations (ToListAsync, FirstOrDefaultAsync, etc.)
    /// Use this when mocking repository methods that return IQueryable and are consumed with EF Core async extensions.
    /// </summary>
    /// <example>
    /// var products = new List&lt;Product&gt; { product1, product2 }.AsAsyncQueryable();
    /// _mockProductRepository.Setup(r => r.FindByCondition(...)).Returns(products);
    /// </example>
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
    {
        return new TestAsyncEnumerable<T>(source);
    }
}
