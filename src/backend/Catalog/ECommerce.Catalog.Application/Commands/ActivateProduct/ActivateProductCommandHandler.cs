using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.ActivateProduct;

public class ActivateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<ActivateProductCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(ActivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.ProductNotFound);

        product.Activate();

        await _products.UpdateAsync(product, cancellationToken);

        var category = await _categories.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        await _products.UpdateAsync(product, cancellationToken);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
