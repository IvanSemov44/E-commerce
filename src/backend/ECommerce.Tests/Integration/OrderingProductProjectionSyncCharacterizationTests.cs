using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderingProductProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task PublishProduct_WhenProjectionMissing_InsertsProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-product-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new ProductProjectionUpdatedIntegrationEvent(productId, "Widget", 19.99m, false, DateTime.UtcNow);
        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
        Assert.AreEqual("Widget", projection.Name);
        Assert.AreEqual(19.99m, projection.Price);
    }

    [TestMethod]
    public async Task PublishProductImage_WhenProjectionMissing_InsertsProjection()
    {
        var imageId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-product-image-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new ProductImageProjectionUpdatedIntegrationEvent(
            imageId,
            productId,
            "https://cdn.example.com/p.jpg",
            true,
            false,
            DateTime.UtcNow);
        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.ProductImages.SingleOrDefaultAsync(x => x.Id == imageId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(productId, projection.ProductId);
        Assert.IsTrue(projection.IsPrimary);
    }

    [TestMethod]
    public async Task PublishDelete_WhenExists_RemovesProductAndImageProjections()
    {
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-product-delete-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Products.Add(new ProductReadModel
        {
            Id = productId,
            Name = "To delete",
            Price = 10m
        });

        db.ProductImages.Add(new ProductImageReadModel
        {
            Id = imageId,
            ProductId = productId,
            Url = "https://cdn.example.com/delete.jpg",
            IsPrimary = true
        });

        await db.SaveChangesAsync();

        await publisher.Publish(new ProductImageProjectionUpdatedIntegrationEvent(
            imageId,
            productId,
            "",
            false,
            true,
            DateTime.UtcNow), CancellationToken.None);

        await publisher.Publish(new ProductProjectionUpdatedIntegrationEvent(
            productId,
            "",
            0m,
            true,
            DateTime.UtcNow), CancellationToken.None);

        Assert.IsNull(await db.ProductImages.SingleOrDefaultAsync(x => x.Id == imageId));
        Assert.IsNull(await db.Products.SingleOrDefaultAsync(x => x.Id == productId));
    }

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<OrderingDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(OrderingDbContext).Assembly));

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
