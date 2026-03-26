using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<UpdateProductCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        var nameResult = ProductName.Create(command.Name);
        if (!nameResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(nameResult.GetErrorOrThrow());

        var productName = nameResult.GetDataOrThrow();
        product.UpdateDetails(productName, command.Description, command.CategoryId);

        await _products.UpdateAsync(product, cancellationToken);

        var dto = product.ToDetailDto(category.Name.Value);

        await _products.UpdateAsync(product, cancellationToken);

        return Result<ProductDetailDto>.Ok(dto);
    }
}
