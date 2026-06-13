using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence.Repositories;

public class PromoCodeRepository(PromotionsDbContext db) : IPromoCodeRepository
{
    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.PromoCodes.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
    {
        var code = PromoCodeString.Reconstitute(normalizedCode);
        return db.PromoCodes.FirstOrDefaultAsync(p => p.Code == code, ct);
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

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpperInvariant();
            // Value-object conversion for Code does not reliably translate string contains across providers.
            var filtered = (await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct))
                .Where(p => p.Code.Value.Contains(searchUpper, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var filteredTotal = filtered.Count;
            var filteredPage = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (filteredPage, filteredTotal);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task AddAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        db.PromoCodes.Add(promoCode);
        return Task.CompletedTask;
    }
}
