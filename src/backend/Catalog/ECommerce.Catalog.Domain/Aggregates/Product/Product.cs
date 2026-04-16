using System;
using System.Collections.Generic;
using System.Linq;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.Aggregates.Product;

public sealed class Product : AggregateRoot
{
    public ProductName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public Money? CompareAtPrice { get; private set; }
    public Sku? Sku { get; private set; }
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }

    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private Product() { }

    public static Result<Product> Create(
        string nameRaw,
        decimal priceAmount,
        string priceCurrency,
        Guid categoryId,
        string? skuRaw = null,
        string? slugRaw = null,
        string? description = null,
        decimal? compareAtPriceAmount = null)
    {
        var nameResult = ProductName.Create(nameRaw);
        if (!nameResult.IsSuccess) return Result<Product>.Fail(nameResult.GetErrorOrThrow());

        var priceResult = Money.Create(priceAmount, priceCurrency);
        if (!priceResult.IsSuccess) return Result<Product>.Fail(priceResult.GetErrorOrThrow());

        Sku? sku = null;
        if (!string.IsNullOrWhiteSpace(skuRaw))
        {
            var skuResult = Sku.Create(skuRaw);
            if (!skuResult.IsSuccess) return Result<Product>.Fail(skuResult.GetErrorOrThrow());
            sku = skuResult.GetDataOrThrow();
        }

        var slugResult = Slug.Create(slugRaw ?? nameRaw);
        if (!slugResult.IsSuccess) return Result<Product>.Fail(slugResult.GetErrorOrThrow());

        Money? compareAtPrice = null;
        if (compareAtPriceAmount.HasValue)
        {
            var compareResult = Money.Create(compareAtPriceAmount.Value, priceCurrency);
            if (!compareResult.IsSuccess) return Result<Product>.Fail(compareResult.GetErrorOrThrow());
            compareAtPrice = compareResult.GetDataOrThrow();
        }

        var name = nameResult.GetDataOrThrow();

        Product product = new()
        {
            Name = name,
            Slug = slugResult.GetDataOrThrow(),
            Price = priceResult.GetDataOrThrow(),
            CompareAtPrice = compareAtPrice,
            Sku = sku,
            Description = description,
            Status = ProductStatus.Draft,
            IsFeatured = false,
            StockQuantity = 0,
            CategoryId = categoryId,
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name.Value, priceResult.GetDataOrThrow().Amount, categoryId));
        return Result<Product>.Ok(product);
    }

    // Takes pre-validated value objects — callers use ProductName.Create() etc. first.
    // Slug derivation from a valid ProductName.Value cannot fail.
    public void UpdateDetails(ProductName name, string? description, Guid categoryId)
    {
        Name = name;
        Slug = Slug.Create(name.Value).GetDataOrThrow();
        Description = description;
        CategoryId = categoryId;
        AddDomainEvent(new ProductUpdatedEvent(Id, name.Value, Price.Amount));
    }

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedEvent(Id, Name.Value, oldPrice, newPrice));
    }

    public Result Activate()
    {
        if (Status != ProductStatus.Active)
            Status = ProductStatus.Active;
        return Result.Ok();
    }

    public Result Deactivate()
    {
        if (Status == ProductStatus.Discontinued)
            return Result.Fail(CatalogErrors.ProductDiscontinued);
        Status = ProductStatus.Inactive;
        AddDomainEvent(new ProductDeactivatedEvent(Id));
        return Result.Ok();
    }

    public void Feature() => IsFeatured = true;
    public void Unfeature() => IsFeatured = false;

    public void Discontinue() => Status = ProductStatus.Discontinued;

    public void Delete()
    {
        Status = ProductStatus.Inactive;
        var snapshots = _images
            .ConvertAll(i => new ProductImageSnapshot(i.Id, i.Url, i.IsPrimary));
        AddDomainEvent(new ProductDeletedEvent(Id, Name.Value, Price.Amount, snapshots));
    }

    public Result AddImage(string url, string? altText)
    {
        if (_images.Count >= 10)
            return Result.Fail(CatalogErrors.ProductMaxImages);
        bool isPrimary = _images.Count == 0;
        int order = _images.Count;
        var imageId = Guid.NewGuid();
        _images.Add(new ProductImage(imageId, Id, url, altText, isPrimary, order));
        AddDomainEvent(new ProductImageAddedEvent(Id, imageId, url, isPrimary));
        return Result.Ok();
    }

    public Result SetPrimaryImage(Guid imageId)
    {
        ProductImage? image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return Result.Fail(CatalogErrors.ProductImageNotFound);
        var oldPrimary = _images.FirstOrDefault(i => i.IsPrimary);
        foreach (ProductImage img in _images) img.SetPrimary(false);
        image.SetPrimary(true);
        AddDomainEvent(new ProductPrimaryImageSetEvent(
            Id,
            image.Id,
            image.Url,
            oldPrimary?.Id,
            oldPrimary?.Url));
        return Result.Ok();
    }

    public Result SetStock(int quantity)
    {
        if (quantity < 0)
            return Result.Fail(CatalogErrors.StockQuantityNegative);
        StockQuantity = quantity;
        return Result.Ok();
    }

}
