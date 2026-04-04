using ECommerce.Infrastructure.Data;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence.Repositories;

public class PromoCodeRepository(AppDbContext db) : IPromoCodeRepository
{
    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.PromoCodes2
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
    {
        return await db.PromoCodes2
            .FirstOrDefaultAsync(p => p.Code.Value == normalizedCode, ct);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PromoCodes2.Where(p => p.IsActive);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        var query = db.PromoCodes2.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Code.Value.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = await db.PromoCodes2.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is null)
        {
            await db.PromoCodes2.AddAsync(promoCode, ct);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(promoCode);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = await db.PromoCodes2.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is not null)
        {
            db.PromoCodes2.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }
}
