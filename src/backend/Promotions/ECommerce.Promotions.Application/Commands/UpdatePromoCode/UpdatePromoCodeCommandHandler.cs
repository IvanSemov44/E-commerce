using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

public sealed class UpdatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<UpdatePromoCodeCommand, Result<PromoCodeDto>>
{
    public async Task<Result<PromoCodeDto>> Handle(UpdatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result<PromoCodeDto>.Fail(PromotionsErrors.PromoNotFound);

        if (command.IsActive.HasValue)
            promoCode.Update(isActive: command.IsActive);

        if (command.DiscountType is not null && command.DiscountValue.HasValue)
        {
            var discountResult = command.DiscountType.ToUpperInvariant() switch
            {
                "PERCENTAGE" => DiscountValue.Percentage(command.DiscountValue.Value),
                "FIXED" => DiscountValue.Fixed(command.DiscountValue.Value),
                _ => DiscountValue.Percentage(command.DiscountValue.Value)
            };
            if (!discountResult.IsSuccess)
                return Result<PromoCodeDto>.Fail(discountResult.GetErrorOrThrow());
            promoCode.Update(discount: discountResult.GetDataOrThrow());
        }

        if (command.ValidFrom.HasValue && command.ValidUntil.HasValue)
        {
            var periodResult = DateRange.Create(command.ValidFrom.Value, command.ValidUntil.Value);
            if (!periodResult.IsSuccess)
                return Result<PromoCodeDto>.Fail(periodResult.GetErrorOrThrow());
            promoCode.Update(validPeriod: periodResult.GetDataOrThrow());
        }

        if (command.MaxUses.HasValue)
            promoCode.Update(maxUses: command.MaxUses);

        if (command.MinimumOrderAmount.HasValue)
            promoCode.Update(minimumOrderAmount: command.MinimumOrderAmount);

        if (command.MaxDiscountAmount.HasValue)
            promoCode.Update(maxDiscountAmount: command.MaxDiscountAmount);

        await repository.UpsertAsync(promoCode, cancellationToken);

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
