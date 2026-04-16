using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateProductPrice;

public class UpdateProductPriceCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<UpdateProductPriceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductPriceCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.ProductNotFound);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var priceResult = Money.Create(command.Price, command.Currency);
        if (!priceResult.IsSuccess)
            return Result<Guid>.Fail(priceResult.GetErrorOrThrow());

        product.UpdatePrice(priceResult.GetDataOrThrow());

        return Result<Guid>.Ok(product.Id);
    }
}
