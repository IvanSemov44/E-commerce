using FluentValidation;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public class CreatePromoCodeCommandValidator : AbstractValidator<CreatePromoCodeCommand>
{
    public CreatePromoCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promo code is required")
            .Length(3, 20).WithMessage("Promo code must be between 3 and 20 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Promo code may only contain letters, digits, and hyphens");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required")
            .Must(x => x.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase) || x.Equals("FIXED", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Discount type must be 'PERCENTAGE' or 'FIXED'");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0")
            .When(x => !string.IsNullOrEmpty(x.DiscountType))
            .Must((cmd, value) =>
            {
                if (cmd.DiscountType?.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase) == true)
                    return value <= 100;
                return true;
            }).WithMessage("Percentage discount cannot exceed 100");

        RuleFor(x => x.ValidFrom)
            .Must((cmd, validFrom) =>
            {
                if (!validFrom.HasValue && !cmd.ValidUntil.HasValue)
                    return true;
                if (validFrom.HasValue && cmd.ValidUntil.HasValue)
                    return validFrom.GetValueOrDefault() < cmd.ValidUntil.GetValueOrDefault();
                return false;
            }).When(x => x.ValidFrom.HasValue || x.ValidUntil.HasValue)
            .WithMessage("Both start and end dates are required, and start date must be before end date");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Max uses must be greater than 0")
            .When(x => x.MaxUses.HasValue);

        RuleFor(x => x.MinimumOrderAmount)
            .GreaterThan(0).WithMessage("Minimum order amount must be greater than 0")
            .When(x => x.MinimumOrderAmount.HasValue);

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0).WithMessage("Max discount amount must be greater than 0")
            .When(x => x.MaxDiscountAmount.HasValue);
    }
}
