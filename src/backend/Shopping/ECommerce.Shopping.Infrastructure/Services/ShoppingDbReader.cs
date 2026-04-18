using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Services;

public class ShoppingDbReader(
    ShoppingDbContext shoppingDb) : IShoppingProductReader, IStockAvailabilityReader
{
    public async Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        var product = await shoppingDb.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.IsActive)
            .Select(p => new { p.Price })
            .FirstOrDefaultAsync(ct);

        if (product is null) return null;
        return new ProductPriceInfo(product.Price, "USD");
    }

    public Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => shoppingDb.InventoryItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Quantity >= quantity, ct);
}
