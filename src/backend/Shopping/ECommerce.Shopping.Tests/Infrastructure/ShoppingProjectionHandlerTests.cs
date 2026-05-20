using ECommerce.Contracts;
using ECommerce.Shopping.Infrastructure.IntegrationEvents;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Shopping.Tests.Infrastructure;

[TestClass]
public class ShoppingProjectionHandlerTests
{
    [TestMethod]
    public async Task CatalogHandler_WhenProductMissing_CreatesProjection()
    {
        await using var db = CreateDb("shopping-catalog-create");
        var handler = new CatalogProductProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<CatalogProductProjectionUpdatedIntegrationEventHandler>.Instance);

        var productId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        await handler.Handle(
            new ProductProjectionUpdatedIntegrationEvent(productId, "name", 42.5m, IsDeleted: false, occurredAt),
            CancellationToken.None);

        var product = await db.Products.SingleAsync(p => p.Id == productId);
        product.Price.ShouldBe(42.5m);
        product.IsActive.ShouldBeTrue();
        product.UpdatedAt.ShouldBe(occurredAt);
    }

    [TestMethod]
    public async Task CatalogHandler_WhenProductExists_UpdatesProjection()
    {
        await using var db = CreateDb("shopping-catalog-update");
        var productId = Guid.NewGuid();
        db.Products.Add(new ProductReadModel
        {
            Id = productId, IsActive = false, Price = 10m, UpdatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var handler = new CatalogProductProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<CatalogProductProjectionUpdatedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ProductProjectionUpdatedIntegrationEvent(productId, "updated", 99m, IsDeleted: false, DateTime.UtcNow),
            CancellationToken.None);

        var product = await db.Products.SingleAsync(p => p.Id == productId);
        product.Price.ShouldBe(99m);
        product.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CatalogHandler_WhenDeleted_RemovesExistingProjection()
    {
        await using var db = CreateDb("shopping-catalog-delete-existing");
        var productId = Guid.NewGuid();
        db.Products.Add(new ProductReadModel
        {
            Id = productId, IsActive = true, Price = 11m, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new CatalogProductProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<CatalogProductProjectionUpdatedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ProductProjectionUpdatedIntegrationEvent(productId, "name", 11m, IsDeleted: true, DateTime.UtcNow),
            CancellationToken.None);

        (await db.Products.AnyAsync(p => p.Id == productId)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task CatalogHandler_WhenDeleteForMissingProduct_IsIdempotent()
    {
        await using var db = CreateDb("shopping-catalog-delete-missing");
        var handler = new CatalogProductProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<CatalogProductProjectionUpdatedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ProductProjectionUpdatedIntegrationEvent(Guid.NewGuid(), "name", 11m, IsDeleted: true, DateTime.UtcNow),
            CancellationToken.None);

        (await db.Products.CountAsync()).ShouldBe(0);
    }

    [TestMethod]
    public async Task InventoryHandler_WhenItemMissing_CreatesProjection()
    {
        await using var db = CreateDb("shopping-stock-create");
        var handler = new InventoryStockProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<InventoryStockProjectionUpdatedIntegrationEventHandler>.Instance);

        var productId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        await handler.Handle(
            new InventoryStockProjectionUpdatedIntegrationEvent(productId, 7, "seed", occurredAt),
            CancellationToken.None);

        var item = await db.InventoryItems.SingleAsync(i => i.ProductId == productId);
        item.Quantity.ShouldBe(7);
        item.UpdatedAt.ShouldBe(occurredAt);
    }

    [TestMethod]
    public async Task InventoryHandler_WhenItemExists_UpdatesProjection()
    {
        await using var db = CreateDb("shopping-stock-update");
        var productId = Guid.NewGuid();
        db.InventoryItems.Add(new InventoryItemReadModel
        {
            ProductId = productId, Quantity = 1, UpdatedAt = DateTime.UtcNow.AddHours(-2)
        });
        await db.SaveChangesAsync();

        var handler = new InventoryStockProjectionUpdatedIntegrationEventHandler(
            db, NullLogger<InventoryStockProjectionUpdatedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new InventoryStockProjectionUpdatedIntegrationEvent(productId, 15, "restock", DateTime.UtcNow),
            CancellationToken.None);

        var item = await db.InventoryItems.SingleAsync(i => i.ProductId == productId);
        item.Quantity.ShouldBe(15);
    }

    private static ShoppingDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ShoppingDbContext>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid():N}")
            .Options;

        return new ShoppingDbContext(options);
    }
}
