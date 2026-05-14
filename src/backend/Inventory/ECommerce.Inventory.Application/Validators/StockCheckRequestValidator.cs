using FluentValidation;
using ECommerce.Inventory.Application.DTOs;

namespace ECommerce.Inventory.Application.Validators;

public class StockCheckRequestValidator : AbstractValidator<StockCheckRequest>
{
    public StockCheckRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item must be provided");

        RuleForEach(x => x.Items).SetValidator(new StockCheckItemDtoValidator());
    }
}

public class StockCheckItemDtoValidator : AbstractValidator<StockCheckItemDto>
{
    public StockCheckItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");
    }
}
