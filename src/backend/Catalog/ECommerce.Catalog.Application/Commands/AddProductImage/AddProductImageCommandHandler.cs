using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.AddProductImage;

public class AddProductImageCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories,
    IProductProjectionEventPublisher? _projectionPublisher = null
) : IRequestHandler<AddProductImageCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(AddProductImageCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        var addResult = product.AddImage(command.Url, command.AltText);
        if (!addResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(addResult.GetErrorOrThrow());

        var addedImage = product.Images.OrderByDescending(x => x.DisplayOrder).FirstOrDefault();

        await _products.UpdateAsync(product, cancellationToken);

        if (_projectionPublisher is not null && addedImage is not null)
        {
            await _projectionPublisher.PublishProductImageProjectionUpdatedAsync(
                addedImage.Id,
                product.Id,
                addedImage.Url,
                addedImage.IsPrimary,
                false,
                cancellationToken);
        }

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
