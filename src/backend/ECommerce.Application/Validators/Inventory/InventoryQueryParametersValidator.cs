using FluentValidation;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Validators.Inventory;

public class InventoryQueryParametersValidator : AbstractValidator<InventoryQueryParameters>
{
    public InventoryQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
