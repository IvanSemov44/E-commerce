using FluentValidation;

namespace ECommerce.Shopping.Application.Validators;

public class AddToCartDtoValidator : AbstractValidator<DTOs.AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}
