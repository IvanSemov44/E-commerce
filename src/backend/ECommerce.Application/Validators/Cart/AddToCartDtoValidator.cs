using FluentValidation;
using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Validators.Cart;

public class AddToCartDtoValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required")
            .Must(id => id != Guid.Empty).WithMessage("ProductId must be a valid GUID");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1");
    }
}
