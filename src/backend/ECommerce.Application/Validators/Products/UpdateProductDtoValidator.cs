using FluentValidation;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Validators.Products;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        When(x => x.CompareAtPrice.HasValue, () =>
        {
            RuleFor(x => x.CompareAtPrice).GreaterThan(x => x.Price).When(x => x.CompareAtPrice.HasValue);
        });
    }
}
