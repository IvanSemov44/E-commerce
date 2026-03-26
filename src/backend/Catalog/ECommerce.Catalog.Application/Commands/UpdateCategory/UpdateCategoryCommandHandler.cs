using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
            return Result<CategoryDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var nameResult = CategoryName.Create(command.Name);
        if (!nameResult.IsSuccess)
            return Result<CategoryDto>.Fail(nameResult.GetErrorOrThrow());

        category.Rename(nameResult.GetDataOrThrow());

        var moveResult = category.MoveTo(command.ParentId);
        if (!moveResult.IsSuccess)
            return Result<CategoryDto>.Fail(moveResult.GetErrorOrThrow());

        return Result<CategoryDto>.Ok(category.ToDto());
    }
}
