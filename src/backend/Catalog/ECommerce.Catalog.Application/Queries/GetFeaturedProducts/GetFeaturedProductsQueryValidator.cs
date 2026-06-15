using FluentValidation;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Catalog.Application.Queries;

public class GetFeaturedProductsQueryValidator : AbstractValidator<GetFeaturedProductsQuery>
{
    public GetFeaturedProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(PaginationConstants.MinPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
    }
}
