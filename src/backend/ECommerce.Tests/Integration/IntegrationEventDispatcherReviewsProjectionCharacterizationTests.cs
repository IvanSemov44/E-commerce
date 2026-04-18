using ECommerce.Contracts;
using ECommerce.Infrastructure.Integration;
using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class IntegrationEventDispatcherReviewsProjectionCharacterizationTests
{
    [TestMethod]
    public async Task DispatchProductProjectionUpdate_WhenProjectionMissing_InsertsProjection()
    {
        var productId = Guid.NewGuid();

        await using var scope = CreateScope($"dispatcher-reviews-insert-{Guid.NewGuid():N}");
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();

        await dispatcher.DispatchAsync(
            new ProductProjectionUpdatedIntegrationEvent(productId, "Widget", 19.99m, false, DateTime.UtcNow),
            CancellationToken.None);

        var projection = await reviewsDb.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
        Assert.IsTrue(projection.IsActive);
    }

    [TestMethod]
    public async Task DispatchProductProjectionUpdate_WhenDuplicateIdempotencyKey_SkipsSecondDelivery()
    {
        var productId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        await using var scope = CreateScope($"dispatcher-reviews-dedupe-{Guid.NewGuid():N}");
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
        var reviewsDb = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();

        var first = new ProductProjectionUpdatedIntegrationEvent(productId, "Widget", 19.99m, false, DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        var duplicateDelete = new ProductProjectionUpdatedIntegrationEvent(productId, "Widget", 19.99m, true, DateTime.UtcNow)
        {
            IdempotencyKey = idempotencyKey
        };

        await dispatcher.DispatchAsync(first, CancellationToken.None);
        await dispatcher.DispatchAsync(duplicateDelete, CancellationToken.None);

        var projection = await reviewsDb.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
    }

    private static AsyncServiceScope CreateScope(string databasePrefix)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ReviewsDbContext>(options =>
            options.UseInMemoryDatabase($"{databasePrefix}-reviews"));
        services.AddDbContext<IntegrationPersistenceDbContext>(options =>
            options.UseInMemoryDatabase($"{databasePrefix}-integration"));
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(ReviewsDbContext).Assembly));
        services.AddScoped<InboxIdempotencyProcessor>();
        services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
