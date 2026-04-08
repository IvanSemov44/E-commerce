using FluentValidation;
using ECommerce.Contracts.DTOs.Inventory;

namespace ECommerce.Contracts.Validators.Inventory;

public class InventoryQueryParametersValidator : AbstractValidator<InventoryQueryParameters>
{
    public InventoryQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

