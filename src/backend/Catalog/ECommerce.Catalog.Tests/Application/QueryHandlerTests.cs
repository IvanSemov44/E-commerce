using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Queries;
using ECommerce.Catalog.Application.Queries;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Tests.Application;

[TestClass]
public class QueryHandlerTests
{
    private static T Unwrap<T>(Result<T> r) => r.GetDataOrThrow();

    sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Store = new();
        public string CategoryNameForDetailLookup = "Cat";
        public int UpdateCallCount;
        public Dictionary<Guid,int> Stock = new();

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Store.FirstOrDefault(p => p.Id == id));
        }

        public Task<bool> SkuExistsAsync(Sku sku, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(p => p.Sku == sku));
        }

        public Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(p => p.Slug == slug));
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            ProductQueryParams p,
            CancellationToken ct = default)
        {
            var items = Store.Skip((p.Page - 1) * p.PageSize).Take(p.PageSize).ToList();
            return Task.FromResult<(IReadOnlyList<Product>, int)>((items, Store.Count));
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            var items = Store.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<Product>, int)>((items, Store.Count));
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetLowStockPagedAsync(int threshold, int page, int pageSize, CancellationToken ct = default)
        {
            var all = Store.Where(p => Stock.TryGetValue(p.Id, out var q) && q <= threshold).ToList();
            var paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<Product>, int)>((paged, all.Count));
        }

        public Task<(Product Product, string CategoryName)?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default)
        {
            var p = Store.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(p is null ? default((Product, string)?) : (p, CategoryNameForDetailLookup));
        }

        public Task<(Product Product, string CategoryName)?> GetBySlugWithCategoryAsync(Slug slug, CancellationToken ct = default)
        {
            var p = Store.FirstOrDefault(x => x.Slug == slug);
            return Task.FromResult(p is null ? default((Product, string)?) : (p, CategoryNameForDetailLookup));
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

    sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Store = new();
        public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Store.FirstOrDefault(c => c.Id == id));
        public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult(Store.FirstOrDefault(c => c.Slug.Value == slug));
        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Category>>(Store.AsReadOnly());
        public Task<(IReadOnlyList<Category> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var items = Store.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var total = Store.Count;
            return Task.FromResult<(IReadOnlyList<Category>, int)>((items, total));
        }
        public Task<(IReadOnlyList<Category> Items, int TotalCount)> GetTopLevelPagedAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var filtered = Store.Where(c => c.ParentId == null).ToList();
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var total = filtered.Count;
            return Task.FromResult<(IReadOnlyList<Category>, int)>((items, total));
        }
        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) => Task.FromResult(Store.Any(c => c.Slug.Value == slug));
        public Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            var result = Store.Where(c => ids.Contains(c.Id)).ToList();
            return Task.FromResult<IReadOnlyList<Category>>(result);
        }
        public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddAsync(Category category, CancellationToken ct = default)
        {
            Store.Add(category);
            return Task.CompletedTask;
        }
    }

    private static Product CreateValidProduct(FakeCategoryRepository categories, FakeProductRepository products, Guid? categoryId = null)
    {
        var catId = categoryId ?? Guid.NewGuid();
        var category = Unwrap(Category.Create("Cat", null));
        if (categoryId != null)
        {
            var existing = categories.Store.FirstOrDefault(c => c.Id == catId);
            if (existing is not null)
                category = existing;
        }
        categories.Store.Add(category);

        var name = Unwrap(ProductName.Create("P"));
        var price = Unwrap(Money.Create(10m, "USD"));
        var sku = Unwrap(Sku.Create(Guid.NewGuid().ToString()));
        var product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, category.Id, sku.Value));
        products.Store.Add(product);
        return product;
    }

    private static Category CreateValidCategory(FakeCategoryRepository categories, Guid? parentId = null)
    {
        var res = Category.Create("Cat", parentId);
        var cat = Unwrap(res);
        categories.Store.Add(cat);
        return cat;
    }

    [TestMethod]
    public async Task GetProductById_ExistingId_ReturnsProductDetailDto()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new GetProductByIdQueryHandler(products);

        var q = new GetProductByIdQuery(product.Id);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(product.Id, res.GetDataOrThrow().Id);
    }

    [TestMethod]
    public async Task GetProductById_UnknownId_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var handler = new GetProductByIdQueryHandler(products);

        var q = new GetProductByIdQuery(Guid.NewGuid());
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetProductById_MissingCategoryName_ReturnsCategoryNotFoundError()
    {
        var products = new FakeProductRepository { CategoryNameForDetailLookup = string.Empty };
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new GetProductByIdQueryHandler(products);

        var res = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetProductBySlug_ExistingSlug_ReturnsProductDetailDto()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new GetProductBySlugQueryHandler(products);

        var q = new GetProductBySlugQuery(product.Slug.Value);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(product.Slug.Value, res.GetDataOrThrow().Slug);
    }

    [TestMethod]
    public async Task GetProductBySlug_UnknownSlug_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var handler = new GetProductBySlugQueryHandler(products);

        var q = new GetProductBySlugQuery("no-such-slug");
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetProductBySlug_MissingCategoryName_ReturnsCategoryNotFoundError()
    {
        var products = new FakeProductRepository { CategoryNameForDetailLookup = string.Empty };
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new GetProductBySlugQueryHandler(products);

        var res = await handler.Handle(new GetProductBySlugQuery(product.Slug.Value), CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetProducts_ReturnsPagedResult()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        for (int i = 0; i < 5; i++) CreateValidProduct(categories, products);
        var handler = new GetProductsQueryHandler(products, categories);

        var q = new GetProductsQuery(1, 10);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var paged = res.GetDataOrThrow();
        Assert.AreEqual(5, paged.TotalCount);
        Assert.HasCount(5, paged.Items);
    }

    [TestMethod]
    public async Task GetFeaturedProducts_ReturnsListOfDtos()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        for (int i = 0; i < 3; i++) CreateValidProduct(categories, products);
        var handler = new GetFeaturedProductsQueryHandler(products, categories);

        var q = new GetFeaturedProductsQuery(Page: 1, PageSize: 2);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var paged = res.GetDataOrThrow();
        Assert.HasCount(2, paged.Items);
        Assert.AreEqual(3, paged.TotalCount);
        Assert.AreEqual(1, paged.Page);
        Assert.AreEqual(2, paged.PageSize);
    }

    // GetCategoriesQuery returns all categories (paginated).
    // Root-only filtering is handled by GetTopLevelCategoriesQuery.
    [TestMethod]
    public async Task GetCategories_ReturnsAllCategoriesPaged()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var root = CreateValidCategory(categories);
        var child = CreateValidCategory(categories, root.Id);
        var handler = new GetCategoriesQueryHandler(categories);

        var q = new GetCategoriesQuery();
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var page = res.GetDataOrThrow();
        Assert.AreEqual(2, page.TotalCount);
        Assert.HasCount(2, page.Items);
        Assert.IsTrue(page.Items.Any(c => c.Id == root.Id));
        Assert.IsTrue(page.Items.Any(c => c.Id == child.Id));
    }

    [TestMethod]
    public async Task GetCategoryBySlug_ExistingSlug_ReturnsCategoryDto()
    {
        var categories = new FakeCategoryRepository();
        var cat = CreateValidCategory(categories);
        var handler = new GetCategoryBySlugQueryHandler(categories);

        var q = new GetCategoryBySlugQuery(cat.Slug.Value);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(cat.Id, res.GetDataOrThrow().Id);
    }

    [TestMethod]
    public async Task GetCategoryBySlug_UnknownSlug_ReturnsCategoryNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        var handler = new GetCategoryBySlugQueryHandler(categories);

        var q = new GetCategoryBySlugQuery("no-such-slug");
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetCategoryById_ExistingId_ReturnsCategoryDto()
    {
        var categories = new FakeCategoryRepository();
        var cat = CreateValidCategory(categories);
        var handler = new GetCategoryByIdQueryHandler(categories);

        var q = new GetCategoryByIdQuery(cat.Id);
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(cat.Id, res.GetDataOrThrow().Id);
    }

    [TestMethod]
    public async Task GetCategoryById_UnknownId_ReturnsCategoryNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        var handler = new GetCategoryByIdQueryHandler(categories);

        var q = new GetCategoryByIdQuery(Guid.NewGuid());
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task GetTopLevelCategories_ReturnsOnlyRootCategories()
    {
        var categories = new FakeCategoryRepository();
        var root = CreateValidCategory(categories);
        var child = CreateValidCategory(categories, root.Id);
        var handler = new GetTopLevelCategoriesQueryHandler(categories);

        var q = new GetTopLevelCategoriesQuery();
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var page = res.GetDataOrThrow();
        var list = page.Items;
        Assert.IsTrue(list.All(c => c.ParentId == null));
        Assert.IsTrue(list.Any(c => c.Id == root.Id));
        Assert.IsFalse(list.Any(c => c.Id == child.Id));
    }

    [TestMethod]
    public async Task GetTopLevelCategories_EmptyStore_ReturnsEmptyList()
    {
        var categories = new FakeCategoryRepository();
        var handler = new GetTopLevelCategoriesQueryHandler(categories);

        var q = new GetTopLevelCategoriesQuery();
        var res = await handler.Handle(q, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var page = res.GetDataOrThrow();
        var list = page.Items;
        Assert.IsEmpty(list);
    }

    [TestMethod]
    public async Task GetLowStockProductsQueryHandler_Handle_ProductsBelowThreshold_ReturnsMatchingProducts()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var p1 = CreateValidProduct(categories, products);
        var p2 = CreateValidProduct(categories, products);
        // mark both as low stock
        products.Stock[p1.Id] = 2;
        products.Stock[p2.Id] = 3;
        var handler = new GetLowStockProductsQueryHandler(products, categories);

        var query = new GetLowStockProductsQuery(5);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var paged = result.GetDataOrThrow();
        Assert.IsGreaterThanOrEqualTo(paged.TotalCount, 2);
        Assert.IsGreaterThanOrEqualTo(paged.Items.Count, 2);
    }

    [TestMethod]
    public async Task GetLowStockProductsQueryHandler_Handle_NoProductsBelowThreshold_ReturnsEmptyList()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new GetLowStockProductsQueryHandler(products, categories);

        var result = await handler.Handle(new GetLowStockProductsQuery(5), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var paged = result.GetDataOrThrow();
        Assert.AreEqual(0, paged.TotalCount);
        Assert.IsEmpty(paged.Items);
    }
}
