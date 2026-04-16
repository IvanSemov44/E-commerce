using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.Errors;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Interfaces;

namespace ECommerce.Catalog.Application.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository _products,
    ICategoryRepository _categories
) : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
            return Result<Guid>.Fail(CatalogApplicationErrors.CategoryNotFound);

        string slugRaw = string.IsNullOrWhiteSpace(command.Slug) ? command.Name : command.Slug;

        var productResult = Product.Create(command.Name, command.Price, command.Currency, command.CategoryId, command.Sku, slugRaw, command.Description, command.CompareAtPrice);
        if (!productResult.IsSuccess)
            return Result<Guid>.Fail(productResult.GetErrorOrThrow());

        var product = productResult.GetDataOrThrow();

        if (await _products.SlugExistsAsync(product.Slug.Value, cancellationToken))
            return Result<Guid>.Fail(CatalogApplicationErrors.DuplicateProductSlug);

        if (product.Sku is not null && await _products.SkuExistsAsync(product.Sku.Value, cancellationToken))
            return Result<Guid>.Fail(CatalogApplicationErrors.SkuAlreadyExists);

        if (command.StockQuantity.HasValue)
        {
            var stockResult = product.SetStock(command.StockQuantity.Value);
            if (!stockResult.IsSuccess)
                return Result<Guid>.Fail(stockResult.GetErrorOrThrow());
        }

        await _products.AddAsync(product, cancellationToken);

        return Result<Guid>.Ok(product.Id);
    }
}
