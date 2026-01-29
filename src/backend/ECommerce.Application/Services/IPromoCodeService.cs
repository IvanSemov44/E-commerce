using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Services;

public interface IPromoCodeService
{
    Task<PaginatedResult<PromoCodeDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<PromoCodeDetailDto?> GetByIdAsync(Guid id);
    Task<PromoCodeDetailDto?> GetByCodeAsync(string code);
    Task<PromoCodeDetailDto> CreateAsync(CreatePromoCodeDto dto);
    Task<PromoCodeDetailDto> UpdateAsync(Guid id, UpdatePromoCodeDto dto);
    Task<bool> DeactivateAsync(Guid id);
    Task<ValidatePromoCodeDto> ValidatePromoCodeAsync(string code, decimal orderAmount);
    Task IncrementUsedCountAsync(Guid promoCodeId);
}
