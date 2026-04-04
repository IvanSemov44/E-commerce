using FluentValidation;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public class DeactivatePromoCodeCommandValidator : AbstractValidator<DeactivatePromoCodeCommand>
{
    public DeactivatePromoCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promo code ID is required");
    }
}
