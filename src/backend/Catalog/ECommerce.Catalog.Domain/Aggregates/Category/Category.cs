using System;
using ECommerce.Catalog.Domain.Aggregates.Category.Events;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.Aggregates.Category;

public sealed class Category : AggregateRoot
{
    public CategoryName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }

    private Category() { }

    public static Result<Category> Create(string nameRaw, Guid? parentId = null, string? slugRaw = null)
    {
        var nameResult = CategoryName.Create(nameRaw);
        if (!nameResult.IsSuccess)
            return Result<Category>.Fail(nameResult.GetErrorOrThrow());

        var slugResult = Slug.Create(slugRaw ?? nameRaw);
        if (!slugResult.IsSuccess)
            return Result<Category>.Fail(slugResult.GetErrorOrThrow());

        var name = nameResult.GetDataOrThrow();
        var slug = slugResult.GetDataOrThrow();

        Category category = new()
        {
            Name = name,
            Slug = slug,
            ParentId = parentId,
            IsActive = true,
        };

        category.AddDomainEvent(new CategoryCreatedEvent(category.Id, name.Value));
        return Result<Category>.Ok(category);
    }

    // Takes a pre-validated CategoryName — callers use CategoryName.Create() first.
    // Slug derivation from a valid CategoryName.Value cannot fail.
    public void Rename(CategoryName newName)
    {
        Name = newName;
        Slug = Slug.Create(newName.Value).GetDataOrThrow();
    }

    public Result UpdateDetails(CategoryName newName, Guid? newParentId)
    {
        if (newParentId == Id)
            return Result.Fail(CatalogErrors.CategoryCircularParent);

        Name = newName;
        Slug = Slug.Create(newName.Value).GetDataOrThrow();
        ParentId = newParentId;
        return Result.Ok();
    }

    public void Deactivate() => IsActive = false;

    public Result MoveTo(Guid? newParentId)
    {
        if (newParentId == Id)
            return Result.Fail(CatalogErrors.CategoryCircularParent);
        ParentId = newParentId;
        return Result.Ok();
    }
}
