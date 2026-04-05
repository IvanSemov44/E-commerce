using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class DbReader(
    OrderingDbContext orderingDb,
    IdentityDbContext identityDb) : IDbReader
{
    public async Task<List<ProductSnapshot>> GetProductsAsync(List<Guid> productIds, CancellationToken ct)
    {
        var products = await orderingDb.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductSnapshot(
                p.Id,
                p.Name,
                p.Price,
                orderingDb.ProductImages
                    .Where(i => i.ProductId == p.Id && i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return products;
    }

    public async Task<(decimal Discount, Guid PromoCodeId)?> GetPromoCodeAsync(string code, CancellationToken ct)
    {
        var promo = await orderingDb.PromoCodes
            .AsNoTracking()
            .Where(p => p.Code == code && p.IsActive)
            .Select(p => new { p.Id, p.DiscountValue })
            .FirstOrDefaultAsync(ct);

        if (promo is null) return null;

        decimal discount = promo.DiscountValue;
        return (discount, promo.Id);
    }

    public async Task<ShippingAddressSnapshot?> GetShippingAddressAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var address = await identityDb.Addresses
            .AsNoTracking()
            .Where(a => a.Id == addressId && a.UserId == userId)
            .Select(a => new ShippingAddressSnapshot(
                a.StreetLine1,
                a.City,
                a.Country,
                a.PostalCode))
            .FirstOrDefaultAsync(ct);

        return address;
    }
}
