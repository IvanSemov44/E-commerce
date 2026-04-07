using FluentValidation;
using ECommerce.Contracts.DTOs.Cart;

namespace ECommerce.Contracts.Validators.Cart;

public class UpdateCartItemDtoValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantity must be at least 1");
    }
}

