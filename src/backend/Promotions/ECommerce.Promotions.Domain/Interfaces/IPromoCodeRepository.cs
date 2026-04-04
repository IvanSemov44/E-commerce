using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Domain.Interfaces;

public interface IPromoCodeRepository
{
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PromoCode?> GetByCodeAsync(string normalizedCode, CancellationToken ct = default);
    Task<(List<PromoCode> Items, int TotalCount)> GetActiveAsync(int page, int pageSize, CancellationToken ct = default);
    Task<(List<PromoCode> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default);
    Task UpsertAsync(PromoCode promoCode, CancellationToken ct = default);
    Task DeleteAsync(PromoCode promoCode, CancellationToken ct = default);
}