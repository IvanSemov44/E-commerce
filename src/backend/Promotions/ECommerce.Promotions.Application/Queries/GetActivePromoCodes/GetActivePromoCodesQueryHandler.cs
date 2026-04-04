using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.DTOs.Common;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetActivePromoCodes;

public sealed class GetActivePromoCodesQueryHandler(IPromoCodeRepository repository) : IRequestHandler<GetActivePromoCodesQuery, Result<PaginatedList<PromoCodeListItemDto>>>
{
    private readonly IPromoCodeRepository _repository = repository;

    public async Task<Result<PaginatedList<PromoCodeListItemDto>>> Handle(GetActivePromoCodesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            request.Page,
            Math.Min(request.PageSize, 100),
            search: null,
            isActive: true,
            cancellationToken);

        var dtos = items.Select(MapToListItem).ToList();

        return Result<PaginatedList<PromoCodeListItemDto>>.Ok(new PaginatedList<PromoCodeListItemDto>(
            dtos,
            totalCount,
            request.Page,
            Math.Min(request.PageSize, 100)));
    }

    private static PromoCodeListItemDto MapToListItem(Domain.Aggregates.PromoCode.PromoCode promoCode)
    {
        return new PromoCodeListItemDto
        {
            Id = promoCode.Id,
            Code = promoCode.Code.Value,
            DiscountType = promoCode.Discount.Type.ToString(),
            DiscountValue = promoCode.Discount.Amount,
            IsActive = promoCode.IsActive,
            UsedCount = promoCode.UsedCount,
            MaxUses = promoCode.MaxUses
        };
    }
}
