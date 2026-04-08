using ECommerce.Contracts;
using ECommerce.Reviews.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ReviewsProductProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task PublishProduct_WhenProjectionMissing_InsertsProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"reviews-product-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.Publish(new ProductProjectionUpdatedIntegrationEvent(productId, "Widget", 19.99m, false, DateTime.UtcNow), CancellationToken.None);

        var projection = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
        Assert.IsTrue(projection.IsActive);
    }

    [TestMethod]
    public async Task PublishDelete_WhenProjectionExists_RemovesProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"reviews-product-delete-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Products.Add(new ProductReadModel
        {
            Id = productId,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await publisher.Publish(new ProductProjectionUpdatedIntegrationEvent(productId, string.Empty, 0m, true, DateTime.UtcNow), CancellationToken.None);

        Assert.IsNull(await db.Products.SingleOrDefaultAsync(x => x.Id == productId));
    }

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ReviewsDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ReviewsDbContext).Assembly));

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
