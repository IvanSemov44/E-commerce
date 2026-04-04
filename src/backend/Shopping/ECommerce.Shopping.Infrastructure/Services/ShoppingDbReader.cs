using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Shopping.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shopping.Infrastructure.Services;

public class ShoppingDbReader(AppDbContext _db) : IShoppingDbReader
{
    public async Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId && p.IsActive)
            .Select(p => new { p.Price, p.Sku })
            .FirstOrDefaultAsync(ct);

        if (product is null) return null;
        var currency = string.IsNullOrEmpty(product.Sku) ? "USD" : "USD";
        return new ProductPriceInfo(product.Price, currency);
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct)
        => await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && p.IsActive, ct);

    public async Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => await _db.InventoryItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId && i.Stock.Quantity >= quantity, ct);
}