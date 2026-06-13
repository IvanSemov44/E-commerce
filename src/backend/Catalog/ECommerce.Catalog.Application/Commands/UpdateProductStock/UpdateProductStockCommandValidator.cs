using FluentValidation;

namespace ECommerce.Catalog.Application.Commands;

public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
{
    public UpdateProductStockCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty();
    }
}
