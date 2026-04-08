using FluentValidation;
using ECommerce.Contracts.DTOs.Inventory;

namespace ECommerce.Contracts.Validators.Inventory;

/// <summary>
/// Validator for StockCheckRequest - validates bulk stock availability checks.
/// </summary>
public class StockCheckRequestValidator : AbstractValidator<StockCheckRequest>
{
    public StockCheckRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item must be provided");

        RuleForEach(x => x.Items).SetValidator(new StockCheckItemDtoValidator());
    }
}

/// <summary>
/// Validator for individual stock check items.
/// </summary>
public class StockCheckItemDtoValidator : AbstractValidator<StockCheckItemDto>
{
    public StockCheckItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

