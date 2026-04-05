using FluentValidation;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Validators;

public class UpdatePromoCodeRequestDtoValidator : AbstractValidator<UpdatePromoCodeRequestDto>
{
    public UpdatePromoCodeRequestDtoValidator()
    {
        RuleFor(x => x.DiscountType)
            .Must(x => x == null || x == "Percentage" || x == "Fixed")
            .WithMessage("Discount type must be 'Percentage' or 'Fixed'");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).When(x => x.DiscountValue.HasValue).WithMessage("Discount value must be greater than 0");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.StartDate)
                .Must((dto, startDate) => startDate < dto.EndDate)
                .WithMessage("Start date must be before end date");
        });

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).When(x => x.MaxUses.HasValue).WithMessage("Max uses must be greater than 0");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue).WithMessage("Min order amount must be >= 0");

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue).WithMessage("Max discount amount must be > 0");
    }
}
