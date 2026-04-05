using FluentValidation;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public class DeletePromoCodeCommandValidator : AbstractValidator<DeletePromoCodeCommand>
{
    public DeletePromoCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Promo code ID is required");
    }
}
