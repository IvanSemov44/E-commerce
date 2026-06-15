using FluentValidation;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Catalog.Application.Queries;

public class GetTopLevelCategoriesQueryValidator : AbstractValidator<GetTopLevelCategoriesQuery>
{
    public GetTopLevelCategoriesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(PaginationConstants.MinPageNumber);
        RuleFor(x => x.PageSize).InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
    }
}
