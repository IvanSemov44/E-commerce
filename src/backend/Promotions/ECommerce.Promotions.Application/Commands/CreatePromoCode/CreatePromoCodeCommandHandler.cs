using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.ValueObjects;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public sealed class CreatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<CreatePromoCodeCommand, Result<PromoCodeDto>>
{
    public async Task<Result<PromoCodeDto>> Handle(CreatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var codeResult = PromoCodeString.Create(command.Code);
        if (!codeResult.IsSuccess)
            return Result<PromoCodeDto>.Fail(codeResult.GetErrorOrThrow());

        var existing = await repository.GetByCodeAsync(codeResult.GetDataOrThrow().Value, cancellationToken);
        if (existing is not null)
            return Result<PromoCodeDto>.Fail(PromotionsErrors.DuplicateCode);

        DiscountValue discount;
        var discountResult = command.DiscountType.ToUpperInvariant() switch
        {
            "PERCENTAGE" => DiscountValue.Percentage(command.DiscountValue),
            "FIXED" => DiscountValue.Fixed(command.DiscountValue),
            _ => DiscountValue.Percentage(command.DiscountValue)
        };

        if (!discountResult.IsSuccess)
            return Result<PromoCodeDto>.Fail(discountResult.GetErrorOrThrow());
        discount = discountResult.GetDataOrThrow();

        DateRange? validPeriod = null;
        if (command.ValidFrom.HasValue && command.ValidUntil.HasValue)
        {
            var periodResult = DateRange.Create(command.ValidFrom.Value, command.ValidUntil.Value);
            if (!periodResult.IsSuccess)
                return Result<PromoCodeDto>.Fail(periodResult.GetErrorOrThrow());
            validPeriod = periodResult.GetDataOrThrow();
        }

        var promoCode = PromoCode.Create(
            codeResult.GetDataOrThrow(),
            discount,
            validPeriod,
            command.MaxUses,
            command.MinimumOrderAmount,
            command.MaxDiscountAmount);

        await repository.UpsertAsync(promoCode, cancellationToken);

        return Result<PromoCodeDto>.Ok(MapToDto(promoCode));
    }

    private static PromoCodeDto MapToDto(PromoCode promoCode)
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
