using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.Aggregates.Category;

namespace ECommerce.Catalog.Application.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    ICategoryRepository _categories
) : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var categoryResult = Category.Create(request.Name, request.ParentId);
        if (!categoryResult.IsSuccess)
            return Result<CategoryDto>.Fail(categoryResult.GetErrorOrThrow());

        var category = categoryResult.GetDataOrThrow();

        await _categories.AddAsync(category, cancellationToken);

        return Result<CategoryDto>.Ok(category.ToDto());
    }
}
