using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.ProductNotFound);

        Guid categoryId = command.CategoryId ?? product.CategoryId;
        var category = await _categories.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var nameResult = ProductName.Create(command.Name);
        if (!nameResult.IsSuccess)
            return Result<Guid>.Fail(nameResult.GetErrorOrThrow());

        product.UpdateDetails(nameResult.GetDataOrThrow(), command.Description, categoryId);

        return Result<Guid>.Ok(product.Id);
    }
}
