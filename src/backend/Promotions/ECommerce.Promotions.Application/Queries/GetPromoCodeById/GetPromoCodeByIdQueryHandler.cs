using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodeById;

public sealed class GetPromoCodeByIdQueryHandler(IPromoCodeRepository repository) : IRequestHandler<GetPromoCodeByIdQuery, Result<PromoCodeDto>>
{
    private readonly IPromoCodeRepository _repository = repository;

    public async Task<Result<PromoCodeDto>> Handle(GetPromoCodeByIdQuery request, CancellationToken cancellationToken)
    {
        var promoCode = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (promoCode is null)
            return Result<PromoCodeDto>.Fail(PromotionsErrors.PromoNotFound);

        return Result<PromoCodeDto>.Ok(MapToDto(promoCode));
    }

    private static PromoCodeDto MapToDto(Domain.Aggregates.PromoCode.PromoCode promoCode)
    {
        return new PromoCodeDto
        {
            Id = promoCode.Id,
            Code = promoCode.Code.Value,
            DiscountType = promoCode.Discount.Type.ToString(),
            DiscountValue = promoCode.Discount.Amount,
            ValidFrom = promoCode.ValidPeriod?.Start,
            ValidUntil = promoCode.ValidPeriod?.End,
            MaxUses = promoCode.MaxUses,
            UsedCount = promoCode.UsedCount,
            IsActive = promoCode.IsActive,
            MinimumOrderAmount = promoCode.MinimumOrderAmount,
            MaxDiscountAmount = promoCode.MaxDiscountAmount,
            CreatedAt = promoCode.CreatedAt,
            UpdatedAt = promoCode.UpdatedAt
        };
    }
}
