using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration;

[TestClass]
public class ShoppingReaderCharacterizationTests
{
    [TestMethod]
    public async Task GetProductPriceAsync_ReturnsPriceInfo_ForActiveProduct()
    {
        var activeProductId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.Products.Add(new ProductReadModel
        {
            Id = activeProductId,
            IsActive = true,
            Price = 49.50m,
            Sku = "SKU-001"
        });
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
        db.Products.Add(new ProductReadModel
        {
            Id = inactiveProductId,
            IsActive = false,
            Price = 10m,
            Sku = "SKU-INACTIVE"
        });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);

        var result = await sut.GetProductPriceAsync(inactiveProductId, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ProductExistsAsync_ReturnsTrueOnlyForActiveProduct()
    {
        var activeProductId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.Products.AddRange(
            new ProductReadModel { Id = activeProductId, IsActive = true, Price = 5m, Sku = "A" },
            new ProductReadModel { Id = inactiveProductId, IsActive = false, Price = 6m, Sku = "B" });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);

        var activeExists = await sut.ProductExistsAsync(activeProductId, CancellationToken.None);
        var inactiveExists = await sut.ProductExistsAsync(inactiveProductId, CancellationToken.None);

        Assert.IsTrue(activeExists);
        Assert.IsFalse(inactiveExists);
    }

    [TestMethod]
    public async Task IsInStockAsync_RespectsRequestedQuantityThreshold()
    {
        var productId = Guid.NewGuid();

        await using var db = CreateShoppingDbContext();
        db.InventoryItems.Add(new InventoryItemReadModel
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = 7
        });
        await db.SaveChangesAsync();

        var sut = new ShoppingDbReader(db);

        var enoughStock = await sut.IsInStockAsync(productId, 5, CancellationToken.None);
        var notEnoughStock = await sut.IsInStockAsync(productId, 8, CancellationToken.None);

        Assert.IsTrue(enoughStock);
        Assert.IsFalse(notEnoughStock);
    }

    private static ShoppingDbContext CreateShoppingDbContext()
    {
        var options = new DbContextOptionsBuilder<ShoppingDbContext>()
            .UseInMemoryDatabase($"shopping-reader-{Guid.NewGuid():N}")
            .Options;

        return new ShoppingDbContext(options);
    }
}
