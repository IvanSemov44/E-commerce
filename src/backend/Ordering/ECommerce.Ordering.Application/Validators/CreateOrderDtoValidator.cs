using FluentValidation;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Contracts.Validators.Orders;

namespace ECommerce.Ordering.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new AddressDtoValidator());
    }
}
