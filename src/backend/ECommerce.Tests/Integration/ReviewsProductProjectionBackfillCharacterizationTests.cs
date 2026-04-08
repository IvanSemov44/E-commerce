using ECommerce.API.Services;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.SharedKernel.Entities;
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
        catalogDb.Categories.Add(new Category { Id = categoryId, Name = "Category", Slug = "category", IsActive = true });
        catalogDb.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "P1",
            Slug = "p1",
            Price = 10m,
            StockQuantity = 2,
            IsActive = true,
            Sku = "SKU-1",
            CategoryId = categoryId
        });
        catalogDb.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "P2",
            Slug = "p2",
            Price = 20m,
            StockQuantity = 3,
            IsActive = false,
            Sku = "SKU-2",
            CategoryId = categoryId
        });
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
        catalogDb.Categories.Add(new Category { Id = categoryId, Name = "Category", Slug = "category", IsActive = true });
        catalogDb.Products.Add(new Product
        {
            Id = productId,
            Name = "P1",
            Slug = "p1",
            Price = 10m,
            StockQuantity = 2,
            IsActive = false,
            Sku = "SKU-1",
            CategoryId = categoryId
        });
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
}
