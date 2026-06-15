using System;
using FluentValidation;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Catalog.Application.Queries;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    private static readonly string[] AllowedSortBy = ["name", "price-asc", "price-desc", "newest", "rating"];

    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(PaginationConstants.MinPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);

        RuleFor(x => x.CategoryId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("CategoryId must not be empty.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x.MinRating)
            .InclusiveBetween(0m, 5m)
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice.Value <= x.MaxPrice.Value)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortBy.Contains(sortBy.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortBy)}");
    }
}
