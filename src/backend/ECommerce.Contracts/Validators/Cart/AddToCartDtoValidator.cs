using FluentValidation;
using ECommerce.Contracts.DTOs.Cart;

namespace ECommerce.Contracts.Validators.Cart;

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

