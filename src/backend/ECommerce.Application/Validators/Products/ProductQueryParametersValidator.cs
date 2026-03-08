using FluentValidation;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Validators.Products;

public class ProductQueryParametersValidator : AbstractValidator<ProductQueryParameters>
{
    public ProductQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice ?? 0)
            .When(x => x.MaxPrice.HasValue && x.MinPrice.HasValue);
        RuleFor(x => x.MinRating).InclusiveBetween(0, 5).When(x => x.MinRating.HasValue);
        RuleFor(x => x.SortBy)
            .Must(x => string.IsNullOrEmpty(x) || new[] { "name", "price-asc", "price-desc", "rating", "newest" }.Contains(x))
            .WithMessage("Invalid sortBy value");
    }
}
