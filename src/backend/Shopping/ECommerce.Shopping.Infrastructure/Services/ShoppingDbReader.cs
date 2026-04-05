using ECommerce.Core.Entities;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Shopping.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Services;

public class ShoppingDbReader(
    CatalogDbContext catalogDb,
    InventoryDbContext inventoryDb) : IShoppingDbReader
{
    public async Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        var product = await catalogDb.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.IsActive)
            .Select(p => new { p.Price, p.Sku })
            .FirstOrDefaultAsync(ct);

        if (product is null) return null;
        var currency = string.IsNullOrEmpty(product.Sku) ? "USD" : "USD";
        return new ProductPriceInfo(product.Price, currency);
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct)
        => await catalogDb.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && p.IsActive, ct);

    public async Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => await inventoryDb.InventoryItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Stock.Quantity >= quantity, ct);
}
