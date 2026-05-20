using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Tests.Infrastructure;

[TestClass]
public class ShoppingDbReaderTests
{
    [TestMethod]
    public async Task GetProductPriceAsync_ReturnsPriceInfo_ForActiveProduct()
    {
        var activeProductId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.Products.Add(new ProductReadModel { Id = activeProductId, IsActive = true, Price = 49.50m });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);
        var result = await sut.GetProductPriceAsync(activeProductId, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(49.50m, result.Price);
        Assert.AreEqual("USD", result.Currency);
    }

    [TestMethod]
    public async Task GetProductPriceAsync_ReturnsNull_ForInactiveProduct()
    {
        var inactiveProductId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.Products.Add(new ProductReadModel { Id = inactiveProductId, IsActive = false, Price = 10m });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);
        var result = await sut.GetProductPriceAsync(inactiveProductId, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetProductPriceAsync_ReturnsPriceOnlyForActiveProduct()
    {
        var activeProductId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.Products.AddRange(
            new ProductReadModel { Id = activeProductId, IsActive = true, Price = 5m },
            new ProductReadModel { Id = inactiveProductId, IsActive = false, Price = 6m });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);

        Assert.IsNotNull(await sut.GetProductPriceAsync(activeProductId, CancellationToken.None));
        Assert.IsNull(await sut.GetProductPriceAsync(inactiveProductId, CancellationToken.None));
    }

    [TestMethod]
    public async Task IsInStockAsync_RespectsRequestedQuantityThreshold()
    {
        var productId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.InventoryItems.Add(new InventoryItemReadModel
        {
            ProductId = productId, Quantity = 7, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);

        Assert.IsTrue(await sut.IsInStockAsync(productId, 5, CancellationToken.None));
        Assert.IsFalse(await sut.IsInStockAsync(productId, 8, CancellationToken.None));
    }

    private static ShoppingDbContext CreateShoppingDbContext()
    {
        var options = new DbContextOptionsBuilder<ShoppingDbContext>()
            .UseInMemoryDatabase($"shopping-reader-{Guid.NewGuid():N}")
            .Options;

        return new ShoppingDbContext(options);
    }
}
