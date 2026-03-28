using FluentValidation;
namespace ECommerce.Catalog.Application.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Sku).MaximumLength(100).When(x => x.Sku != null);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
