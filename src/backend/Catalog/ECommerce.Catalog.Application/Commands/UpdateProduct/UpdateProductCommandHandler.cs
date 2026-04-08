using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Application.Interfaces;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories,
    IProductProjectionEventPublisher? _projectionPublisher = null
) : IRequestHandler<UpdateProductCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        Guid categoryId = command.CategoryId ?? product.CategoryId;
        var category = await _categories.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var nameResult = ProductName.Create(command.Name);
        if (!nameResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(nameResult.GetErrorOrThrow());

        product.UpdateDetails(nameResult.GetDataOrThrow(), command.Description, categoryId);

        if (_projectionPublisher is not null)
        {
            await _projectionPublisher.PublishProductProjectionUpdatedAsync(
                product.Id,
                product.Name.Value,
                product.Price.Amount,
                product.IsDeleted,
                cancellationToken);
        }

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
