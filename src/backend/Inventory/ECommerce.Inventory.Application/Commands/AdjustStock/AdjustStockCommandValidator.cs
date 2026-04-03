using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}