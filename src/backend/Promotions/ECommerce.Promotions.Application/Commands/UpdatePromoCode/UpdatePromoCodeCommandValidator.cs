using FluentValidation;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

public class UpdatePromoCodeCommandValidator : AbstractValidator<UpdatePromoCodeCommand>
{
    public UpdatePromoCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promo code ID is required");

        RuleFor(x => x.DiscountType)
            .Must(x => x == null || x.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase) || x.Equals("FIXED", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Discount type must be 'PERCENTAGE' or 'FIXED'")
            .When(x => !string.IsNullOrEmpty(x.DiscountType));

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0")
            .When(x => x.DiscountValue.HasValue)
            .Must((cmd, value) =>
            {
                // If DiscountType is specified and is PERCENTAGE, validate percentage range
                if (cmd.DiscountType?.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase) == true)
                    return value <= 100;
                return true;
            }).When(x => x.DiscountValue.HasValue && !string.IsNullOrEmpty(x.DiscountType))
            .WithMessage("Percentage discount cannot exceed 100");

        RuleFor(x => x)
            .Must(cmd =>
            {
                // If one of DiscountType or DiscountValue is provided, both must be provided
                var hasType = !string.IsNullOrEmpty(cmd.DiscountType);
                var hasValue = cmd.DiscountValue.HasValue;
                return (hasType && hasValue) || (!hasType && !hasValue);
            })
            .WithMessage("Both discount type and discount value must be provided together")
            .When(x => !string.IsNullOrEmpty(x.DiscountType) || x.DiscountValue.HasValue);

        RuleFor(x => x)
            .Must(cmd =>
            {
                // If one of ValidFrom or ValidUntil is provided, both must be provided and ValidFrom < ValidUntil
                if (!cmd.ValidFrom.HasValue && !cmd.ValidUntil.HasValue)
                    return true;
                if (cmd.ValidFrom.HasValue && cmd.ValidUntil.HasValue)
                    return cmd.ValidFrom.GetValueOrDefault() < cmd.ValidUntil.GetValueOrDefault();
                return false;
            })
            .WithMessage("Both start and end dates are required if updating date range, and start date must be before end date")
            .When(x => x.ValidFrom.HasValue || x.ValidUntil.HasValue);

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
