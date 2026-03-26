using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
            return Result.Fail(CatalogApplicationErrors.CategoryNotFound);

        bool hasProducts = await _categories.HasProductsAsync(command.Id, cancellationToken);
        if (hasProducts)
            return Result.Fail(CatalogApplicationErrors.CategoryHasProducts);

        await _categories.DeleteAsync(category, cancellationToken);

        return Result.Ok();
    }
}
