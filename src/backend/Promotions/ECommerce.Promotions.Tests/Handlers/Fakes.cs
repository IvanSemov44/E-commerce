using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Application.Interfaces;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Tests.Handlers;

public sealed class FakePromoCodeRepository : IPromoCodeRepository
{
    private readonly Dictionary<Guid, PromoCode> _store = new();

    public void Seed(PromoCode promoCode) => _store[promoCode.Id] = promoCode;

    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(p => p.Code.Value == normalizedCode));

    public Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var active = _store.Values.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt).ToList();
        var items = active.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, active.Count));
    }

    public Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken ct = default)
    {
        var query = _store.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Value.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var ordered = query.OrderByDescending(p => p.CreatedAt).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, ordered.Count));
    }

    public Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        _store[promoCode.Id] = promoCode;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default)
    {
        _store.Remove(promoCode.Id);
        return Task.CompletedTask;
    }

    public bool Contains(Guid id) => _store.ContainsKey(id);
}

public sealed class FakePromoProjectionEventPublisher : IPromoProjectionEventPublisher
{
    public Task PublishPromoProjectionUpdatedAsync(
        Guid promoCodeId,
        string code,
        decimal discountValue,
        bool isActive,
        bool isDeleted,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
