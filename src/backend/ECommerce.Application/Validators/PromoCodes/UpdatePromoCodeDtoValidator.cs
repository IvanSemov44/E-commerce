using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Core.Enums;

namespace ECommerce.Application.Validators.PromoCodes;

/// <summary>
/// Validator for UpdatePromoCodeDto - validates promo code updates with optional fields.
/// </summary>
public class UpdatePromoCodeDtoValidator : AbstractValidator<UpdatePromoCodeDto>
{
    public UpdatePromoCodeDtoValidator()
    {
        RuleFor(x => x.Code)
            .Length(1, 50).WithMessage("Promo code must be between 1 and 50 characters")
            .Matches("^[A-Z0-9-]+$").WithMessage("Promo code must contain only uppercase letters, numbers, and hyphens")
            .When(x => x.Code != null);

        RuleFor(x => x.DiscountType)
            .Must(t => Enum.TryParse<DiscountType>(t, ignoreCase: true, out _))
            .WithMessage("Discount type must be 'percentage' or 'fixed'")
            .When(x => x.DiscountType != null);

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0")
            .When(x => x.DiscountValue.HasValue);

        RuleFor(x => x.MinOrderAmount)
            .GreaterThan(0).WithMessage("Minimum order amount must be greater than 0")
            .When(x => x.MinOrderAmount.HasValue);

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0).WithMessage("Maximum discount amount must be greater than 0")
            .When(x => x.MaxDiscountAmount.HasValue);

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Maximum uses must be greater than 0")
            .When(x => x.MaxUses.HasValue);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
