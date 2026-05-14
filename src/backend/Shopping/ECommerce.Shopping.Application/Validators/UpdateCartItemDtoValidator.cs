using FluentValidation;

namespace ECommerce.Shopping.Application.Validators;

public class UpdateCartItemDtoValidator : AbstractValidator<DTOs.UpdateCartItemDto>
{
    public UpdateCartItemDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}
