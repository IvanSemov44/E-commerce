using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Application.Commands.CreateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProduct;
using ECommerce.Catalog.Application.Commands.DeleteProduct;
using ECommerce.Catalog.Application.Commands.UpdateProductPrice;
using ECommerce.Catalog.Application.Commands.ActivateProduct;
using ECommerce.Catalog.Application.Commands.DeactivateProduct;
using ECommerce.Catalog.Application.Commands.UpdateProductStock;
using ECommerce.Catalog.Application.Commands.AddProductImage;
using ECommerce.Catalog.Application.Commands.SetPrimaryImage;
using ECommerce.Catalog.Application.Commands.CreateCategory;
using ECommerce.Catalog.Application.Commands.UpdateCategory;
using ECommerce.Catalog.Application.Commands.DeleteCategory;
namespace ECommerce.Catalog.Tests.Application;

[TestClass]
public class CommandHandlerTests
{
    private static T Unwrap<T>(Result<T> r) => r.GetDataOrThrow();

    sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Store = new();

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Store.FirstOrDefault(p => p.Id == id));
        }

        public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            return Task.FromResult(Store.FirstOrDefault(p => p.Slug.Value == slug));
        }

        public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(p => p.Sku?.Value == sku));
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(p => p.Slug.Value == slug));
        }

        public Task<bool> ExistsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(p => p.CategoryId == categoryId));
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Guid? categoryId = null,
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            decimal? minRating = null,
            bool? isFeatured = null,
            string? sortBy = null,
            CancellationToken ct = default)
        {
            return Task.FromResult<(IReadOnlyList<Product>, int)>((Store.AsReadOnly(), Store.Count));
        }

        public Task<IReadOnlyList<Product>> GetFeaturedAsync(int limit, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Store.Take(limit).ToList());
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetFeaturedPagedAsync(
            int page, int pageSize, CancellationToken ct = default)
        {
            var items = Store.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<Product>, int)>((items, Store.Count));
        }

        public Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Store.ToList());
        }

        public Task<int> GetActiveProductsCountAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Store.Count);
        }

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            Store.Add(product);
            return Task.CompletedTask;
        }
    }

    sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Store = new();
        public bool HasProductsReturn;

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Store.FirstOrDefault(c => c.Id == id));
        }

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            return Task.FromResult(Store.FirstOrDefault(c => c.Slug.Value == slug));
        }

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(Store.AsReadOnly());
        }
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

        public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        {
            return Task.FromResult(Store.Any(c => c.Slug.Value == slug));
        }

        public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct = default)
        {
            return Task.FromResult(HasProductsReturn);
        }

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
            // prefer an existing category with the requested id if present
            var existing = categories.Store.FirstOrDefault(c => c.Id == catId);
            if (existing is not null)
                category = existing;
            else
            {
                // fall back to the freshly-created category; do not attempt brittle reflection to set Id
            }
        }
        categories.Store.Add(category);

        var name = Unwrap(ProductName.Create("P"));
        var price = Unwrap(Money.Create(10m, "USD"));
        var sku = Unwrap(Sku.Create("SKU1"));
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
    public async Task CreateProductCommandHandler_Handle_ValidCommand_ReturnsCreatedProductId()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var category = CreateValidCategory(categories);
        var handler = new CreateProductCommandHandler(products, categories);

        var cmd = new CreateProductCommand("Name", 5m, category.Id, Sku: "SKU-123");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var productId = res.GetDataOrThrow();
        var created = products.Store.Single(p => p.Id == productId);
        Assert.AreEqual("Name", created.Name.Value);
        Assert.AreEqual("SKU-123", created.Sku!.Value);
    }

    [TestMethod]
    public async Task CreateProductCommandHandler_Handle_CategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new CreateProductCommandHandler(products, categories);

        var cmd = new CreateProductCommand("Name", 5m, Guid.NewGuid(), Sku: "SKU-123");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task CreateProductCommandHandler_Handle_DuplicateSku_ReturnsSkuAlreadyExistsError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var category = CreateValidCategory(categories);
        var p = CreateValidProduct(categories, products, category.Id);
        var handler = new CreateProductCommandHandler(products, categories);

        var cmd = new CreateProductCommand("Name2", 5m, category.Id, Sku: p.Sku!.Value);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("SKU_ALREADY_EXISTS", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task CreateProductCommandHandler_Handle_InvalidProductName_ReturnsDomainFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var category = CreateValidCategory(categories);
        var handler = new CreateProductCommandHandler(products, categories);

        var cmd = new CreateProductCommand("", 5m, category.Id, Sku: "SKU-99");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NAME_EMPTY", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductCommandHandler_Handle_ValidCommand_UpdatesProductAndReturnsId()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var category = CreateValidCategory(categories);
        var product = CreateValidProduct(categories, products, category.Id);
        var handler = new UpdateProductCommandHandler(products, categories);

        var cmd = new UpdateProductCommand(product.Id, "NewName", "Desc", category.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var updatedProductId = res.GetDataOrThrow();
        Assert.AreEqual(product.Id, updatedProductId);
        Assert.AreEqual("NewName", product.Name.Value);
    }

    [TestMethod]
    public async Task UpdateProductCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new UpdateProductCommandHandler(products, categories);

        var cmd = new UpdateProductCommand(Guid.NewGuid(), "N", null, Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductCommandHandler_Handle_CategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new UpdateProductCommandHandler(products, categories);

        var cmd = new UpdateProductCommand(product.Id, "N", null, Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task DeleteProductCommandHandler_Handle_ExistingProduct_DeletesAndReturnsOk()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new DeleteProductCommandHandler(products);

        var cmd = new DeleteProductCommand(product.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var deleted = products.Store.First(p => p.Id == product.Id);
        Assert.AreEqual(ProductStatus.Inactive, deleted.Status);
    }

    [TestMethod]
    public async Task DeleteProductCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var handler = new DeleteProductCommandHandler(products);

        var cmd = new DeleteProductCommand(Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductPriceCommandHandler_Handle_ValidPrice_UpdatesPriceAndReturnsId()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new UpdateProductPriceCommandHandler(products, categories);

        var cmd = new UpdateProductPriceCommand(product.Id, 20m, "USD");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var updatedProductId = res.GetDataOrThrow();
        Assert.AreEqual(product.Id, updatedProductId);
        Assert.AreEqual(20m, product.Price.Amount);
    }

    [TestMethod]
    public async Task UpdateProductPriceCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new UpdateProductPriceCommandHandler(products, categories);

        var cmd = new UpdateProductPriceCommand(Guid.NewGuid(), 20m, "USD");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductPriceCommandHandler_Handle_InvalidPrice_ReturnsDomainFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new UpdateProductPriceCommandHandler(products, categories);

        var cmd = new UpdateProductPriceCommand(product.Id, -5m, "USD");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("MONEY_NEGATIVE", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductPriceCommandHandler_Handle_CategoryMissing_DoesNotMutatePrice()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        categories.Store.Clear();
        var originalPrice = product.Price.Amount;
        var handler = new UpdateProductPriceCommandHandler(products, categories);

        var cmd = new UpdateProductPriceCommand(product.Id, 20m, "USD");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
        Assert.AreEqual(originalPrice, product.Price.Amount);
    }

    [TestMethod]
    public async Task ActivateProductCommandHandler_Handle_DraftProduct_ReturnsActiveStatus()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new ActivateProductCommandHandler(products);

        var cmd = new ActivateProductCommand(product.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(ProductStatus.Active, product.Status);
    }

    [TestMethod]
    public async Task ActivateProductCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new ActivateProductCommandHandler(products);

        var cmd = new ActivateProductCommand(Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task DeactivateProductCommandHandler_Handle_ActiveProduct_ReturnsInactiveStatus()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        product.Activate();
        var handler = new DeactivateProductCommandHandler(products);

        var cmd = new DeactivateProductCommand(product.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        Assert.AreEqual(ProductStatus.Inactive, product.Status);
    }

    [TestMethod]
    public async Task DeactivateProductCommandHandler_Handle_DiscontinuedProduct_ReturnsDomainFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        product.Discontinue();

        var handler = new DeactivateProductCommandHandler(products);
        var cmd = new DeactivateProductCommand(product.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_DISCONTINUED", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task DeactivateProductCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new DeactivateProductCommandHandler(products);

        var cmd = new DeactivateProductCommand(Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductStockCommandHandler_Handle_ValidCommand_UpdatesStockAndReturnsOk()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new UpdateProductStockCommandHandler(products);

        var cmd = new UpdateProductStockCommand(product.Id, 25, "restock");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
    }

    [TestMethod]
    public async Task UpdateProductStockCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var handler = new UpdateProductStockCommandHandler(products);

        var cmd = new UpdateProductStockCommand(Guid.NewGuid(), 10, "reason");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateProductStockCommandHandler_Handle_NegativeQuantity_ReturnsStockQuantityNegativeError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new UpdateProductStockCommandHandler(products);

        var cmd = new UpdateProductStockCommand(product.Id, -1, "bad");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("STOCK_QUANTITY_NEGATIVE", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task AddProductImageCommandHandler_Handle_ValidImage_AddsImageAndReturnsId()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new AddProductImageCommandHandler(products, categories);

        var cmd = new AddProductImageCommand(product.Id, "http://img", "alt");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var updatedProductId = res.GetDataOrThrow();
        Assert.AreEqual(product.Id, updatedProductId);
        Assert.IsTrue(product.Images.Any());
    }

    [TestMethod]
    public async Task AddProductImageCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new AddProductImageCommandHandler(products, categories);

        var cmd = new AddProductImageCommand(Guid.NewGuid(), "http://", null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task AddProductImageCommandHandler_Handle_MaxImagesReached_ReturnsDomainFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        for (int i = 0; i < 10; i++) product.AddImage($"http://{i}", null);
        var handler = new AddProductImageCommandHandler(products, categories);

        var cmd = new AddProductImageCommand(product.Id, "http://too-many", null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_MAX_IMAGES", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task AddProductImageCommandHandler_Handle_CategoryMissing_DoesNotAddImage()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        categories.Store.Clear();
        int imageCountBefore = product.Images.Count;
        var handler = new AddProductImageCommandHandler(products, categories);

        var cmd = new AddProductImageCommand(product.Id, "http://img", "alt");
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
        Assert.AreEqual(imageCountBefore, product.Images.Count);
    }

    [TestMethod]
    public async Task SetPrimaryImageCommandHandler_Handle_ValidImageId_SetsPrimaryAndReturnsId()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        product.AddImage("http://a", null);
        product.AddImage("http://b", null);
        var id = product.Images.Skip(1).First().Id;
        var handler = new SetPrimaryImageCommandHandler(products, categories);

        var cmd = new SetPrimaryImageCommand(product.Id, id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var updatedProductId = res.GetDataOrThrow();
        Assert.AreEqual(product.Id, updatedProductId);
        Assert.IsTrue(product.Images.Any(i => i.Id == id && i.IsPrimary));
    }

    [TestMethod]
    public async Task SetPrimaryImageCommandHandler_Handle_ProductNotFound_ReturnsProductNotFoundError()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var handler = new SetPrimaryImageCommandHandler(products, categories);

        var cmd = new SetPrimaryImageCommand(Guid.NewGuid(), Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("PRODUCT_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task SetPrimaryImageCommandHandler_Handle_UnknownImageId_ReturnsDomainFailure()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        var handler = new SetPrimaryImageCommandHandler(products, categories);

        var cmd = new SetPrimaryImageCommand(product.Id, Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("IMAGE_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task SetPrimaryImageCommandHandler_Handle_CategoryMissing_DoesNotChangePrimary()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var product = CreateValidProduct(categories, products);
        product.AddImage("http://a", null);
        product.AddImage("http://b", null);
        var imageId = product.Images.Skip(1).First().Id;
        bool wasPrimaryBefore = product.Images.First(i => i.Id == imageId).IsPrimary;
        categories.Store.Clear();
        var handler = new SetPrimaryImageCommandHandler(products, categories);

        var cmd = new SetPrimaryImageCommand(product.Id, imageId);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
        Assert.AreEqual(wasPrimaryBefore, product.Images.First(i => i.Id == imageId).IsPrimary);
    }

    [TestMethod]
    public async Task CreateCategoryCommandHandler_Handle_ValidCommand_ReturnsCreatedCategoryId()
    {
        var categories = new FakeCategoryRepository();
        var handler = new CreateCategoryCommandHandler(categories);

        var cmd = new CreateCategoryCommand("NewCat", null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var categoryId = res.GetDataOrThrow();
        var created = categories.Store.Single(c => c.Id == categoryId);
        Assert.AreEqual("NewCat", created.Name.Value);
    }

    [TestMethod]
    public async Task CreateCategoryCommandHandler_Handle_InvalidName_ReturnsDomainFailure()
    {
        var categories = new FakeCategoryRepository();
        var handler = new CreateCategoryCommandHandler(categories);

        var cmd = new CreateCategoryCommand("", null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NAME_EMPTY", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateCategoryCommandHandler_Handle_ValidCommand_UpdatesTrackedAggregateAndReturnsId()
    {
        var categories = new FakeCategoryRepository();
        var parent = CreateValidCategory(categories);
        var cat = CreateValidCategory(categories);
        var handler = new UpdateCategoryCommandHandler(categories);

        var cmd = new UpdateCategoryCommand(cat.Id, "Renamed", parent.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var updatedCategoryId = res.GetDataOrThrow();
        Assert.AreEqual(cat.Id, updatedCategoryId);
        Assert.AreEqual("Renamed", cat.Name.Value);
    }

    [TestMethod]
    public async Task UpdateCategoryCommandHandler_Handle_CategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        var handler = new UpdateCategoryCommandHandler(categories);

        var cmd = new UpdateCategoryCommand(Guid.NewGuid(), "N", null);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task UpdateCategoryCommandHandler_Handle_CircularParent_ReturnsDomainFailure()
    {
        var categories = new FakeCategoryRepository();
        var cat = CreateValidCategory(categories);
        var originalName = cat.Name.Value;
        var handler = new UpdateCategoryCommandHandler(categories);

        var cmd = new UpdateCategoryCommand(cat.Id, "N", cat.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_CIRCULAR", res.GetErrorOrThrow().Code);
        Assert.AreEqual(originalName, cat.Name.Value);
    }

    [TestMethod]
    public async Task DeleteCategoryCommandHandler_Handle_EmptyCategory_DeactivatesAndReturnsOk()
    {
        var categories = new FakeCategoryRepository();
        var cat = CreateValidCategory(categories);
        var handler = new DeleteCategoryCommandHandler(categories);

        var cmd = new DeleteCategoryCommand(cat.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsTrue(res.IsSuccess);
        var stored = categories.Store.Single(c => c.Id == cat.Id);
        Assert.IsFalse(stored.IsActive);
    }

    [TestMethod]
    public async Task DeleteCategoryCommandHandler_Handle_CategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var categories = new FakeCategoryRepository();
        var handler = new DeleteCategoryCommandHandler(categories);

        var cmd = new DeleteCategoryCommand(Guid.NewGuid());
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_NOT_FOUND", res.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task DeleteCategoryCommandHandler_Handle_CategoryHasProducts_ReturnsCategoryHasProductsError()
    {
        var categories = new FakeCategoryRepository();
        var cat = CreateValidCategory(categories);
        categories.HasProductsReturn = true;
        var handler = new DeleteCategoryCommandHandler(categories);

        var cmd = new DeleteCategoryCommand(cat.Id);
        var res = await handler.Handle(cmd, CancellationToken.None);

        Assert.IsFalse(res.IsSuccess);
        Assert.AreEqual("CATEGORY_HAS_PRODUCTS", res.GetErrorOrThrow().Code);
    }
}
