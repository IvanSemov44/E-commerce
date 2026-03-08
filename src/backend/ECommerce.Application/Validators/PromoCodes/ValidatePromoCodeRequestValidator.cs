using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Validators.PromoCodes;

/// <summary>
/// Validator for ValidatePromoCodeRequestDto - validates promo code verification requests.
/// </summary>
public class ValidatePromoCodeRequestDtoValidator : AbstractValidator<ValidatePromoCodeRequestDto>
{
    public ValidatePromoCodeRequestDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promo code is required")
            .Length(1, 50).WithMessage("Promo code must be between 1 and 50 characters")
            .Matches("^[A-Z0-9-]+$").WithMessage("Promo code must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.OrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Order amount must be greater than or equal to 0");
    }
}
