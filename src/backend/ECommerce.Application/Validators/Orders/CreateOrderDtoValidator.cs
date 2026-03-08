using FluentValidation;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Validators.Orders;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new AddressDtoValidator());
    }
}
