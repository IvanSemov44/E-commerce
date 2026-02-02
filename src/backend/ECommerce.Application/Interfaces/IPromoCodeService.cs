using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing promotional codes and discounts.
/// </summary>
public interface IPromoCodeService
{
    Task<PaginatedResult<PromoCodeDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto> CreateAsync(CreatePromoCodeDto dto, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto> UpdateAsync(Guid id, UpdatePromoCodeDto dto, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ValidatePromoCodeDto> ValidatePromoCodeAsync(string code, decimal orderAmount, CancellationToken cancellationToken = default);
    Task IncrementUsedCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default);
}
