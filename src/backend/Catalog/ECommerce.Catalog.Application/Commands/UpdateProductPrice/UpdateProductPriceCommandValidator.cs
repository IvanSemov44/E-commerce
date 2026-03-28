using FluentValidation;
namespace ECommerce.Catalog.Application.Commands.UpdateProductPrice;

public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceCommandValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
