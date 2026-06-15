using FluentValidation;
namespace ECommerce.Catalog.Application.Commands;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Sku).MaximumLength(100).When(x => x.Sku != null);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue);
        RuleFor(x => x)
            .Must(x => !x.CompareAtPrice.HasValue || x.CompareAtPrice.Value > x.Price)
            .WithName("CompareAtPrice")
            .WithMessage("CompareAtPrice must be greater than Price.");
    }
}
