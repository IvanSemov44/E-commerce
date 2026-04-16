using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.SetPrimaryImage;

public class SetPrimaryImageCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<SetPrimaryImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SetPrimaryImageCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.ProductNotFound);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var setResult = product.SetPrimaryImage(command.ImageId);
        if (!setResult.IsSuccess)
            return Result<Guid>.Fail(setResult.GetErrorOrThrow());

        return Result<Guid>.Ok(product.Id);
    }
}
