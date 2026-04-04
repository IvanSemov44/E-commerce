using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Application.DTOs.Common;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodes;

public sealed class GetPromoCodesQueryHandler(IPromoCodeRepository repository) : IRequestHandler<GetPromoCodesQuery, Result<PaginatedList<PromoCodeListItemDto>>>
{
    private readonly IPromoCodeRepository _repository = repository;

    public async Task<Result<PaginatedList<PromoCodeListItemDto>>> Handle(GetPromoCodesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            request.Page,
            Math.Min(request.PageSize, 100),
            request.Search,
            request.IsActive,
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
