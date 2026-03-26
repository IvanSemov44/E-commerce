using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<CreateProductCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.CategoryNotFound);

        bool skuExists = await _products.SkuExistsAsync(command.Sku, cancellationToken);
        if (skuExists)
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.SkuAlreadyExists);

        var productResult = Product.Create(command.Name, command.Price, command.Currency, command.Sku, command.CategoryId, command.Description, command.CompareAtPrice);
        if (!productResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(productResult.GetErrorOrThrow());

        var product = productResult.GetDataOrThrow();
        await _products.AddAsync(product, cancellationToken);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
