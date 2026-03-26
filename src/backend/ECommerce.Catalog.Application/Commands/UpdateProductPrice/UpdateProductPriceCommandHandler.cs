using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateProductPrice;

public class UpdateProductPriceCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<UpdateProductPriceCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(UpdateProductPriceCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        var priceResult = Money.Create(command.Price, command.Currency);
        if (!priceResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(priceResult.GetErrorOrThrow());

        product.UpdatePrice(priceResult.GetDataOrThrow());

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
