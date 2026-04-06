using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OrderingAddressReadModel = ECommerce.Ordering.Infrastructure.Persistence.AddressReadModel;
using OrderingDbContext = ECommerce.Ordering.Infrastructure.Persistence.OrderingDbContext;
using OrderingDbReader = ECommerce.Ordering.Infrastructure.Persistence.DbReader;
using OrderingProductImageReadModel = ECommerce.Ordering.Infrastructure.Persistence.ProductImageReadModel;
using OrderingProductReadModel = ECommerce.Ordering.Infrastructure.Persistence.ProductReadModel;
using OrderingPromoCodeReadModel = ECommerce.Ordering.Infrastructure.Persistence.PromoCodeReadModel;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderingReaderCharacterizationTests
{
    [TestMethod]
    public async Task GetProductsAsync_ReturnsRequestedProducts_WithPrimaryImagesOnly()
    {
        var productWithPrimaryImageId = Guid.NewGuid();
        var productWithoutPrimaryImageId = Guid.NewGuid();
        var nonRequestedProductId = Guid.NewGuid();

        await using var db = CreateOrderingDbContext();
        db.Products.AddRange(
            new OrderingProductReadModel
            {
                Id = productWithPrimaryImageId,
                Name = "Primary Image Product",
                Price = 19.99m
            },
            new OrderingProductReadModel
            {
                Id = productWithoutPrimaryImageId,
                Name = "No Primary Image Product",
                Price = 29.99m
            },
            new OrderingProductReadModel
            {
                Id = nonRequestedProductId,
                Name = "Not Requested",
                Price = 39.99m
            });

        db.ProductImages.AddRange(
            new OrderingProductImageReadModel
            {
                Id = Guid.NewGuid(),
                ProductId = productWithPrimaryImageId,
                Url = "https://cdn.example.com/primary.jpg",
                IsPrimary = true
            },
            new OrderingProductImageReadModel
            {
                Id = Guid.NewGuid(),
                ProductId = productWithPrimaryImageId,
                Url = "https://cdn.example.com/secondary.jpg",
                IsPrimary = false
            },
            new OrderingProductImageReadModel
            {
                Id = Guid.NewGuid(),
                ProductId = productWithoutPrimaryImageId,
                Url = "https://cdn.example.com/non-primary.jpg",
                IsPrimary = false
            });

        await db.SaveChangesAsync();

        var sut = new OrderingDbReader(db);

        var result = await sut.GetProductsAsync(
            new List<Guid> { productWithPrimaryImageId, productWithoutPrimaryImageId },
            CancellationToken.None);

        Assert.AreEqual(2, result.Count);

        var withPrimaryImage = result.Single(x => x.ProductId == productWithPrimaryImageId);
        Assert.AreEqual("Primary Image Product", withPrimaryImage.ProductName);
        Assert.AreEqual(19.99m, withPrimaryImage.UnitPrice);
        Assert.AreEqual("https://cdn.example.com/primary.jpg", withPrimaryImage.ImageUrl);

        var withoutPrimaryImage = result.Single(x => x.ProductId == productWithoutPrimaryImageId);
        Assert.AreEqual("No Primary Image Product", withoutPrimaryImage.ProductName);
        Assert.AreEqual(29.99m, withoutPrimaryImage.UnitPrice);
        Assert.IsNull(withoutPrimaryImage.ImageUrl);
    }

    [TestMethod]
    public async Task GetShippingAddressAsync_ReturnsAddress_ForMatchingUserAndAddress()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        await using var db = CreateOrderingDbContext();
        db.Addresses.Add(new OrderingAddressReadModel
        {
            Id = addressId,
            UserId = userId,
            StreetLine1 = "42 Reader St",
            City = "Testville",
            Country = "US",
            PostalCode = "10001",
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new OrderingDbReader(db);

        var result = await sut.GetShippingAddressAsync(userId, addressId, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual("42 Reader St", result.Street);
        Assert.AreEqual("Testville", result.City);
        Assert.AreEqual("US", result.Country);
        Assert.AreEqual("10001", result.PostalCode);
    }

    [TestMethod]
    public async Task GetPromoCodeAsync_WhenNoMatchingPromoCode_ReturnsNull()
    {
        await using var db = CreateOrderingDbContext();
        var sut = new OrderingDbReader(db);

        var result = await sut.GetPromoCodeAsync("MISSING", CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetPromoCodeAsync_WhenActivePromoCodeExists_ReturnsDiscountAndId()
    {
        var promoId = Guid.NewGuid();

        await using var db = CreateOrderingDbContext();
        db.PromoCodes.Add(new OrderingPromoCodeReadModel
        {
            Id = promoId,
            Code = "SAVE10",
            DiscountValue = 10m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new OrderingDbReader(db);

        var result = await sut.GetPromoCodeAsync("SAVE10", CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(10m, result.Value.Discount);
        Assert.AreEqual(promoId, result.Value.PromoCodeId);
    }

    private static OrderingDbContext CreateOrderingDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseInMemoryDatabase($"ordering-reader-{Guid.NewGuid():N}")
            .Options;

        return new OrderingDbContext(options);
    }
}
