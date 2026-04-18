using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class CatalogProductRatingProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task PublishRating_WhenProjectionMissing_InsertsProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"catalog-rating-insert-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.Publish(
            new ProductRatingProjectionUpdatedIntegrationEvent(productId, 4.5m, 12, DateTime.UtcNow),
            CancellationToken.None);

        var projection = await db.ProductRatings.SingleOrDefaultAsync(x => x.ProductId == productId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(4.5m, projection.AverageRating);
        Assert.AreEqual(12, projection.ReviewCount);
    }

    [TestMethod]
    public async Task PublishRating_WhenProjectionExists_UpdatesProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"catalog-rating-update-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.ProductRatings.Add(new ProductRatingReadModel
        {
            ProductId = productId,
            AverageRating = 2.0m,
            ReviewCount = 1,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var updatedAt = DateTime.UtcNow;
        await publisher.Publish(
            new ProductRatingProjectionUpdatedIntegrationEvent(productId, 4.8m, 42, updatedAt),
            CancellationToken.None);

        var projection = await db.ProductRatings.SingleOrDefaultAsync(x => x.ProductId == productId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(4.8m, projection.AverageRating);
        Assert.AreEqual(42, projection.ReviewCount);
        Assert.AreEqual(updatedAt, projection.UpdatedAt);
    }

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(CatalogDbContext).Assembly));

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
