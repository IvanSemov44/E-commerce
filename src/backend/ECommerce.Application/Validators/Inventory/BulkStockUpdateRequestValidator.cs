using FluentValidation;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Validators.Inventory;

public class BulkStockUpdateRequestValidator : AbstractValidator<BulkStockUpdateRequest>
{
    public BulkStockUpdateRequestValidator()
    {
        RuleFor(x => x.Updates)
            .NotNull().WithMessage("Updates list is required")
            .NotEmpty().WithMessage("At least one update is required")
            .Must(updates => updates.Count <= 100).WithMessage("Cannot update more than 100 products at once");

        RuleForEach(x => x.Updates).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            item.RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative");
        });
    }
}
