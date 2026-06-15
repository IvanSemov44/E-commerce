using FluentValidation;
using ECommerce.SharedKernel.Constants;

namespace ECommerce.Catalog.Application.Queries;

public class GetLowStockProductsQueryValidator : AbstractValidator<GetLowStockProductsQuery>
{
    public GetLowStockProductsQueryValidator()
    {
        RuleFor(x => x.Threshold)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(PaginationConstants.MinPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(PaginationConstants.MinPageSize, PaginationConstants.MaxPageSize);
    }
}
