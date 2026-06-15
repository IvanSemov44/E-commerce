using FluentValidation;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Catalog.Application.Queries;

public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(PaginationConstants.MinPageNumber);
        RuleFor(x => x.PageSize).InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
    }
}
