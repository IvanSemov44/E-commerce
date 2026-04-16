using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.AddProductImage;

public class AddProductImageCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<AddProductImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddProductImageCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.ProductNotFound);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var addResult = product.AddImage(command.Url, command.AltText);
        if (!addResult.IsSuccess)
            return Result<Guid>.Fail(addResult.GetErrorOrThrow());

        return Result<Guid>.Ok(product.Id);
    }
}
