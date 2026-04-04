using FluentValidation;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}