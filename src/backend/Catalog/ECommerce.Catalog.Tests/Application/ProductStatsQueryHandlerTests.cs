using ECommerce.Catalog.Application.Queries;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Queries;
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

        public Task<bool> SkuExistsAsync(Sku sku, CancellationToken ct = default)
            => Task.FromResult(Store.Any(p => p.Sku == sku));

        public Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default)
            => Task.FromResult(Store.Any(p => p.Slug == slug));

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQueryParams p, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Product>, int)>((Store, Store.Count));

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Product>, int)>((Store, Store.Count));

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetLowStockPagedAsync(int threshold, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<Product>, int)>((Store, Store.Count));

        public Task<(Product Product, string CategoryName)?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default)
        {
            var p = Store.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(p is null ? default((Product, string)?) : (p, string.Empty));
        }

        public Task<(Product Product, string CategoryName)?> GetBySlugWithCategoryAsync(Slug slug, CancellationToken ct = default)
        {
            var p = Store.FirstOrDefault(x => x.Slug == slug);
            return Task.FromResult(p is null ? default((Product, string)?) : (p, string.Empty));
        }

        public Task<IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)>> GetRatingsByProductIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, (decimal, int)>>(new Dictionary<Guid, (decimal, int)>());

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
