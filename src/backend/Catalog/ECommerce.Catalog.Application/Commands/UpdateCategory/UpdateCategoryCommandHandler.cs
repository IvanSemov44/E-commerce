using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<UpdateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var nameResult = CategoryName.Create(command.Name);
        if (!nameResult.IsSuccess)
            return Result<Guid>.Fail(nameResult.GetErrorOrThrow());

        var updateResult = category.UpdateDetails(nameResult.GetDataOrThrow(), command.ParentId);
        if (!updateResult.IsSuccess)
            return Result<Guid>.Fail(updateResult.GetErrorOrThrow());

        return Result<Guid>.Ok(category.Id);
    }
}
