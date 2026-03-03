using FluentValidation;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Validators.Inventory;

/// <summary>
/// Validator for AdjustStockRequest - validates stock adjustment operations.
/// </summary>
public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    private static readonly HashSet<string> ValidReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "restock", "adjustment", "damage", "correction", "return", "sample"
    };

    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage("Quantity is required")
            .Must(q => q != 0).WithMessage("Quantity must not be zero");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .Must(r => ValidReasons.Contains(r))
            .WithMessage("Reason must be one of: restock, adjustment, damage, correction, return, sample");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters")
            .When(x => x.Notes != null);
    }
}
