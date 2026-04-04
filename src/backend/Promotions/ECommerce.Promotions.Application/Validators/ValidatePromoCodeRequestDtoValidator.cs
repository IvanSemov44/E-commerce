using FluentValidation;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Validators;

public class ValidatePromoCodeRequestDtoValidator : AbstractValidator<ValidatePromoCodeRequestDto>
{
    public ValidatePromoCodeRequestDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required");

        RuleFor(x => x.OrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Order amount must be greater than or equal to 0");
    }
}
