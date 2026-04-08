using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderingPromoProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task Handle_WhenProjectionMissing_InsertsProjection()
    {
        var promoId = Guid.NewGuid();
        var databaseName = $"ordering-promo-proj-{Guid.NewGuid():N}";

        await using var scope = CreateScope(databaseName);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE15",
            15m,
            true,
            false,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.PromoCodes.SingleOrDefaultAsync(x => x.Id == promoId);
        Assert.IsNotNull(projection);
        Assert.AreEqual("SAVE15", projection.Code);
        Assert.AreEqual(15m, projection.DiscountValue);
        Assert.IsTrue(projection.IsActive);
    }

    [TestMethod]
    public async Task Handle_WhenProjectionExists_UpdatesProjection()
    {
        var promoId = Guid.NewGuid();
        var databaseName = $"ordering-promo-proj-{Guid.NewGuid():N}";

        await using var scope = CreateScope(databaseName);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.PromoCodes.Add(new PromoCodeReadModel
        {
            Id = promoId,
            Code = "SAVE10",
            DiscountValue = 10m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE10",
            25m,
            false,
            false,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.PromoCodes.SingleOrDefaultAsync(x => x.Id == promoId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(25m, projection.DiscountValue);
        Assert.IsFalse(projection.IsActive);
    }

    [TestMethod]
    public async Task Handle_WhenDeleted_RemovesProjection()
    {
        var promoId = Guid.NewGuid();
        var databaseName = $"ordering-promo-proj-{Guid.NewGuid():N}";

        await using var scope = CreateScope(databaseName);
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.PromoCodes.Add(new PromoCodeReadModel
        {
            Id = promoId,
            Code = "SAVE20",
            DiscountValue = 20m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE20",
            20m,
            true,
            true,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.PromoCodes.SingleOrDefaultAsync(x => x.Id == promoId);
        Assert.IsNull(projection);
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
