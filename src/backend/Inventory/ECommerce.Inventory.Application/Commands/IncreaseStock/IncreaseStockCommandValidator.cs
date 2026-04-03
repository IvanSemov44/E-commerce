using FluentValidation;

namespace ECommerce.Inventory.Application.Commands.IncreaseStock;

public class IncreaseStockCommandValidator : AbstractValidator<IncreaseStockCommand>
{
    public IncreaseStockCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}