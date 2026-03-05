using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Core.Results;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for managing promotional codes and discounts.
/// </summary>
public interface IPromoCodeService
{
    Task<PaginatedResult<PromoCodeDto>> GetAllAsync(PromoCodeQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PromoCodeDetailDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<PromoCodeDto>> GetActiveCodesAsync(CancellationToken cancellationToken = default);
    Task<Result<PromoCodeDetailDto>> CreateAsync(CreatePromoCodeDto dto, CancellationToken cancellationToken = default);
    Task<Result<PromoCodeDetailDto>> UpdateAsync(Guid id, UpdatePromoCodeDto dto, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ValidatePromoCodeDto> ValidatePromoCodeAsync(string code, decimal orderAmount, CancellationToken cancellationToken = default);
    Task<Result<Unit>> IncrementUsedCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default);
}
