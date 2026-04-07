using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Promotions.Infrastructure.Persistence.Repositories;

public class PromoCodeRepository(PromotionsDbContext db) : IPromoCodeRepository
{
    private bool IsInMemory => db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (IsInMemory)
        {
            return Task.FromResult(db.PromoCodes.AsEnumerable().FirstOrDefault(p => p.Id == id));
        }

        return db.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
    {
        if (IsInMemory)
        {
            return Task.FromResult(db.PromoCodes.AsEnumerable().FirstOrDefault(p => p.Code.Value == normalizedCode));
        }

        return db.PromoCodes
            .FirstOrDefaultAsync(p => EF.Property<string>(p, nameof(PromoCode.Code)) == normalizedCode, ct);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (IsInMemory)
        {
            var query = db.PromoCodes.AsEnumerable().Where(p => p.IsActive);
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        var relationalQuery = db.PromoCodes.Where(p => p.IsActive);

        var relationalTotalCount = await relationalQuery.CountAsync(ct);

        var relationalItems = await relationalQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (relationalItems, relationalTotalCount);
    }

    public async Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        if (IsInMemory)
        {
            IEnumerable<PromoCode> query = db.PromoCodes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Code.Value.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        var queryable = db.PromoCodes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryable = queryable.Where(p => EF.Property<string>(p, nameof(PromoCode.Code)).Contains(search));
        }

        if (isActive.HasValue)
        {
            queryable = queryable.Where(p => p.IsActive == isActive.Value);
        }

        var totalCountRelational = await queryable.CountAsync(ct);

        var itemsRelational = await queryable
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (itemsRelational, totalCountRelational);
    }

    public async Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = IsInMemory
            ? db.PromoCodes.AsEnumerable().FirstOrDefault(p => p.Id == promoCode.Id)
            : await db.PromoCodes.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is null)
        {
            await db.PromoCodes.AddAsync(promoCode, ct);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(promoCode);
        }
    }

    public async Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        var existing = IsInMemory
            ? db.PromoCodes.AsEnumerable().FirstOrDefault(p => p.Id == promoCode.Id)
            : await db.PromoCodes.FindAsync([promoCode.Id], cancellationToken: ct);

        if (existing is not null)
        {
            db.PromoCodes.Remove(existing);
        }
    }
}
