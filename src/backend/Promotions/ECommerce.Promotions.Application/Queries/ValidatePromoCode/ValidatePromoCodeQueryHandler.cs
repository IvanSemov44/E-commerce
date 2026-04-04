using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Services;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Application.Queries.ValidatePromoCode;

public sealed class ValidatePromoCodeQueryHandler(IPromoCodeRepository repository)
    : IRequestHandler<ValidatePromoCodeQuery, Result<ValidatePromoCodeResultDto>>
{
    public async Task<Result<ValidatePromoCodeResultDto>> Handle(ValidatePromoCodeQuery request, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        var codeResult = PromoCodeString.Create(request.Code);
        if (!codeResult.IsSuccess)
        {
            return Result<ValidatePromoCodeResultDto>.Ok(new ValidatePromoCodeResultDto
            {
                IsValid = false,
                DiscountAmount = 0,
                Message = "Invalid promo code format"
            });
        }

        var codeValue = codeResult.GetDataOrThrow().Value;
        var promoCode = await repository.GetByCodeAsync(codeValue, cancellationToken);
        if (promoCode is null)
        {
            return Result<ValidatePromoCodeResultDto>.Ok(new ValidatePromoCodeResultDto
            {
                Code = codeValue,
                IsValid = false,
                DiscountAmount = 0,
                Message = "Promo code not found"
            });
        }

        var calcResult = DiscountCalculator.Calculate(promoCode, request.OrderAmount, now);

        if (!calcResult.IsSuccess)
        {
            return Result<ValidatePromoCodeResultDto>.Ok(new ValidatePromoCodeResultDto
            {
                PromoCodeId = promoCode.Id,
                Code = promoCode.Code.Value,
                IsValid = false,
                DiscountAmount = 0,
                Message = calcResult.GetErrorOrThrow().Message
            });
        }

        var calculation = calcResult.GetDataOrThrow();
        return Result<ValidatePromoCodeResultDto>.Ok(new ValidatePromoCodeResultDto
        {
            PromoCodeId = promoCode.Id,
            Code = promoCode.Code.Value,
            IsValid = true,
            DiscountAmount = calculation.DiscountAmount,
            Message = "Promo code applied successfully"
        });
    }
}
