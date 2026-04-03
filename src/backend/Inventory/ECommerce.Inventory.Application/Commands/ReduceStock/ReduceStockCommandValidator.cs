using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.ReduceStock;

public class ReduceStockCommandValidator : AbstractValidator<ReduceStockCommand>
{
    public ReduceStockCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}