using ECommerce.API.Services;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ReviewsProductProjectionBackfillCharacterizationTests
{
    [TestMethod]
    public async Task Backfill_WhenProjectionMissing_InsertsProjectionRows()
    {
        await using var scope = CreateScope($"reviews-backfill-missing-{Guid.NewGuid():N}");
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ReviewsProductProjectionBackfillService>();

        var categoryId = Guid.NewGuid();
        var categoryResult = Category.Create("Category", null, "category");
        Assert.IsTrue(categoryResult.IsSuccess);
        var category = categoryResult.GetDataOrThrow();
        SetEntityId(category, categoryId);
        catalogDb.Categories.Add(category);

        var p1Result = Product.Create("P1", 10m, "USD", categoryId, "SKU-1", "p1");
        Assert.IsTrue(p1Result.IsSuccess);
        var p1 = p1Result.GetDataOrThrow();
        p1.SetStock(2);
        p1.Activate();
        catalogDb.Products.Add(p1);

        var p2Result = Product.Create("P2", 20m, "USD", categoryId, "SKU-2", "p2");
        Assert.IsTrue(p2Result.IsSuccess);
        var p2 = p2Result.GetDataOrThrow();
        p2.SetStock(3);
        catalogDb.Products.Add(p2);
        await catalogDb.SaveChangesAsync();

        var result = await service.BackfillAsync();

        Assert.AreEqual(2, result.CatalogProductCount);
        Assert.AreEqual(2, result.InsertedCount);
        Assert.AreEqual(0, result.UpdatedCount);
        Assert.AreEqual(2, await reviewsDb.Products.CountAsync());
    }

    [TestMethod]
    public async Task Backfill_WhenProjectionExists_UpdatesIsActiveWithoutDeletingExtraRows()
    {
        await using var scope = CreateScope($"reviews-backfill-update-{Guid.NewGuid():N}");
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ReviewsProductProjectionBackfillService>();

        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var categoryResult = Category.Create("Category", null, "category");
        Assert.IsTrue(categoryResult.IsSuccess);
        var category = categoryResult.GetDataOrThrow();
        SetEntityId(category, categoryId);
        catalogDb.Categories.Add(category);

        var productResult = Product.Create("P1", 10m, "USD", categoryId, "SKU-1", "p1");
        Assert.IsTrue(productResult.IsSuccess);
        var product = productResult.GetDataOrThrow();
        SetEntityId(product, productId);
        product.SetStock(2);
        catalogDb.Products.Add(product);
        await catalogDb.SaveChangesAsync();

        reviewsDb.Products.Add(new ProductReadModel
        {
            Id = productId,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        reviewsDb.Products.Add(new ProductReadModel
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await reviewsDb.SaveChangesAsync();

        var result = await service.BackfillAsync();

        Assert.AreEqual(1, result.CatalogProductCount);
        Assert.AreEqual(0, result.InsertedCount);
        Assert.AreEqual(1, result.UpdatedCount);

        var projection = await reviewsDb.Products.SingleAsync(x => x.Id == productId);
        Assert.IsFalse(projection.IsActive);
        Assert.AreEqual(2, await reviewsDb.Products.CountAsync());
    }

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ReviewsDbContext>(options => options.UseInMemoryDatabase($"{databaseName}-reviews"));
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase($"{databaseName}-catalog"));
        services.AddScoped<ReviewsProductProjectionBackfillService>();

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }

    private static void SetEntityId(object entity, Guid id)
    {
        var property = entity.GetType().BaseType?.GetProperty("Id") ?? entity.GetType().GetProperty("Id");
        property?.SetValue(entity, id);
    }
}
