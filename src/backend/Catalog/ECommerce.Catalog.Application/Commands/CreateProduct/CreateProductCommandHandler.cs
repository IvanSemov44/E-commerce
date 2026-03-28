using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Application.DTOs.Products;
using ECommerce.Catalog.Application.Extensions;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;
using ECommerce.Catalog.Domain.ValueObjects;

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

        // Validate name first so domain errors surface before slug processing
        var nameValidation = ProductName.Create(command.Name);
        if (!nameValidation.IsSuccess)
            return Result<ProductDetailDto>.Fail(nameValidation.GetErrorOrThrow());

        // Determine slug (explicit or derived from name) and check uniqueness
        string slugRaw = string.IsNullOrWhiteSpace(command.Slug) ? command.Name : command.Slug;
        var slugValidation = Slug.Create(slugRaw);
        if (!slugValidation.IsSuccess)
            return Result<ProductDetailDto>.Fail(slugValidation.GetErrorOrThrow());

        string slug = slugValidation.GetDataOrThrow().Value;
        if (await _products.SlugExistsAsync(slug, cancellationToken))
            return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.DuplicateProductSlug);

        if (!string.IsNullOrWhiteSpace(command.Sku))
        {
            if (await _products.SkuExistsAsync(command.Sku, cancellationToken))
                return Result<ProductDetailDto>.Fail(CatalogApplicationErrors.SkuAlreadyExists);
        }

        var productResult = Product.Create(command.Name, command.Price, command.Currency, command.CategoryId, command.Sku, slugRaw, command.Description, command.CompareAtPrice);
        if (!productResult.IsSuccess)
            return Result<ProductDetailDto>.Fail(productResult.GetErrorOrThrow());

        var product = productResult.GetDataOrThrow();

        if (command.StockQuantity.HasValue)
        {
            var stockResult = product.SetStock(command.StockQuantity.Value);
            if (!stockResult.IsSuccess)
                return Result<ProductDetailDto>.Fail(stockResult.GetErrorOrThrow());
        }

        await _products.AddAsync(product, cancellationToken);

        return Result<ProductDetailDto>.Ok(product.ToDetailDto(category.Name.Value));
    }
}
