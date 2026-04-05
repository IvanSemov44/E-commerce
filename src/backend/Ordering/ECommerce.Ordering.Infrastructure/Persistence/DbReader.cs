using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public class DbReader(
    CatalogDbContext catalogDb,
    PromotionsDbContext promotionsDb,
    IdentityDbContext identityDb) : IDbReader
{
    public async Task<List<ProductSnapshot>> GetProductsAsync(List<Guid> productIds, CancellationToken ct)
    {
        var products = await catalogDb.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new ProductSnapshot(
                p.Id,
                p.Name,
                p.Price,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(ct);

        return products;
    }

    public async Task<(decimal Discount, Guid PromoCodeId)?> GetPromoCodeAsync(string code, CancellationToken ct)
    {
        var promo = await promotionsDb.PromoCodes
            .AsNoTracking()
            .Where(p => p.Code.Value == code && p.IsActive)
            .Select(p => new { p.Id, p.Discount })
            .FirstOrDefaultAsync(ct);

        if (promo is null) return null;

        decimal discount = promo.Discount.Amount;
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
