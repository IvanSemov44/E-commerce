using ECommerce.Catalog.Application.Queries.GetProductStats;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Tests.Application;

[TestClass]
public class ProductStatsQueryHandlerTests
{
    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Store = new();

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(p => p.Id == id));

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(p => p.Slug.Value == slug));

        public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
            => Task.FromResult(Store.Any(p => p.Sku?.Value == sku));

        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Store.Any(p => p.Slug.Value == slug));

        public Task<bool> ExistsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
            => Task.FromResult(Store.Any(p => p.CategoryId == categoryId));

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Guid? categoryId = null, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minRating = null, bool? isFeatured = null, string? sortBy = null, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Product>, int)>((Store, Store.Count));

        public Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(Store.Take(limit).ToList());

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Product>, int)>((Store, Store.Count));

        public Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Product>>(Store);

        public Task<int> GetActiveProductsCountAsync(CancellationToken ct = default)
            => Task.FromResult(Store.Count);

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            Store.Add(product);
            return Task.CompletedTask;
        }

    }

    [TestMethod]
    public async Task Handle_ReturnsActiveProductCount()
    {
        var repo = new FakeProductRepository();
        await repo.AddAsync(CreateProduct("Product 1", "SKU-1"));
        await repo.AddAsync(CreateProduct("Product 2", "SKU-2"));

        var handler = new GetProductStatsQueryHandler(repo);
        var result = await handler.Handle(new GetProductStatsQuery(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.GetDataOrThrow().TotalProducts);
    }

    private static Product CreateProduct(string nameValue, string skuValue)
    {
        var name = ProductName.Create(nameValue).GetDataOrThrow();
        var price = Money.Create(10m, "USD").GetDataOrThrow();
        var sku = Sku.Create(skuValue).GetDataOrThrow();

        return Product.Create(name.Value, price.Amount, price.Currency, Guid.NewGuid(), sku.Value).GetDataOrThrow();
    }
}
