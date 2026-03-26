using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.SetPrimaryImage;

public class SetPrimaryImageCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<SetPrimaryImageCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(SetPrimaryImageCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        var setResult = product.SetPrimaryImage(command.ImageId);
        if (!setResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(setResult.GetErrorOrThrow());

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
