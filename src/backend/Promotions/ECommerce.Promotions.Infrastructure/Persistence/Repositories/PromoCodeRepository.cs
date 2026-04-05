using ECommerce.Infrastructure.Data;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence.Repositories;

public class PromoCodeRepository(AppDbContext db) : IPromoCodeRepository
{
    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return db.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
    {
        return db.PromoCodes
            .FirstOrDefaultAsync(p => p.Code.Value == normalizedCode, ct);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PromoCodes.Where(p => p.IsActive);

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
        var query = db.PromoCodes.AsQueryable();

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
        var existing = await db.PromoCodes.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is null)
        {
            await db.PromoCodes.AddAsync(promoCode, ct);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(promoCode);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = await db.PromoCodes.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is not null)
        {
            db.PromoCodes.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }
}
