using FluentValidation;
using ECommerce.Contracts.DTOs.Orders;

namespace ECommerce.Contracts.Validators.Orders;

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

