using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.IntegrationEvents;
using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderingPromoProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task Handle_WhenProjectionMissing_InsertsProjection()
    {
        var promoId = Guid.NewGuid();

        await using var db = CreateOrderingDbContext();
        var sut = new PromoCodeProjectionUpdatedIntegrationEventHandler(db);

        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE15",
            15m,
            true,
            false,
            DateTime.UtcNow);

        await sut.Handle(evt, CancellationToken.None);

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

        await using var db = CreateOrderingDbContext();
        db.PromoCodes.Add(new PromoCodeReadModel
        {
            Id = promoId,
            Code = "SAVE10",
            DiscountValue = 10m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var sut = new PromoCodeProjectionUpdatedIntegrationEventHandler(db);
        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE10",
            25m,
            false,
            false,
            DateTime.UtcNow);

        await sut.Handle(evt, CancellationToken.None);

        var projection = await db.PromoCodes.SingleOrDefaultAsync(x => x.Id == promoId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(25m, projection.DiscountValue);
        Assert.IsFalse(projection.IsActive);
    }

    [TestMethod]
    public async Task Handle_WhenDeleted_RemovesProjection()
    {
        var promoId = Guid.NewGuid();

        await using var db = CreateOrderingDbContext();
        db.PromoCodes.Add(new PromoCodeReadModel
        {
            Id = promoId,
            Code = "SAVE20",
            DiscountValue = 20m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new PromoCodeProjectionUpdatedIntegrationEventHandler(db);
        var evt = new PromoCodeProjectionUpdatedIntegrationEvent(
            promoId,
            "SAVE20",
            20m,
            true,
            true,
            DateTime.UtcNow);

        await sut.Handle(evt, CancellationToken.None);

        var projection = await db.PromoCodes.SingleOrDefaultAsync(x => x.Id == promoId);
        Assert.IsNull(projection);
    }

    private static OrderingDbContext CreateOrderingDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase($"ordering-promo-proj-{Guid.NewGuid():N}")
            .Options;

        return new OrderingDbContext(options);
    }
}
