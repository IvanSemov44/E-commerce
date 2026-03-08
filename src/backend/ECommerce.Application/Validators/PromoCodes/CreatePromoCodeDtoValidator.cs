using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Core.Enums;

namespace ECommerce.Application.Validators.PromoCodes;

public class CreatePromoCodeDtoValidator : AbstractValidator<CreatePromoCodeDto>
{
    public CreatePromoCodeDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promo code is required")
            .MaximumLength(50).WithMessage("Promo code cannot exceed 50 characters")
            .Matches("^[A-Z0-9]+$").WithMessage("Promo code must contain only uppercase letters and numbers");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required")
            .Must(x => Enum.TryParse<DiscountType>(x, ignoreCase: true, out _))
            .WithMessage("Discount type must be 'percentage' or 'fixed'");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0");

        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => string.Equals(x.DiscountType, "percentage", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Percentage discount cannot exceed 100%");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount cannot be negative");

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0)
            .When(x => x.MaxDiscountAmount.HasValue)
            .WithMessage("Maximum discount amount must be greater than 0");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0)
            .When(x => x.MaxUses.HasValue)
            .WithMessage("Maximum uses must be greater than 0");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }
}
