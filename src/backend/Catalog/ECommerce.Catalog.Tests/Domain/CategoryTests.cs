using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.Aggregates.Category.Events;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Tests.Domain;

[TestClass]
public class CategoryTests
{
    private static T Unwrap<T>(Result<T> result) => result.GetDataOrThrow();

    [TestMethod]
    public void ApiContract_Rename_UsesValueObjectOverloadOnly()
    {
        MethodInfo? valueObjectMethod = typeof(Category).GetMethod(
            "Rename",
            [typeof(CategoryName)]);

        MethodInfo? primitiveMethod = typeof(Category).GetMethod(
            "Rename",
            [typeof(string)]);

        Assert.IsNotNull(valueObjectMethod);
        Assert.IsNull(primitiveMethod);
    }

    [TestMethod]
    public void CategoryName_EmptyString_ReturnsFailure()
    {
        // Arrange
        string raw = "";
        // Act
        var res1 = CategoryName.Create(raw);
        // Assert
        Assert.IsFalse(res1.IsSuccess);
    }

    [TestMethod]
    public void CategoryName_ExceedsMaxLength_ReturnsFailure()
    {
        // Arrange
        string raw = new string('a', 101);
        // Act
        var res2 = CategoryName.Create(raw);
        // Assert
        Assert.IsFalse(res2.IsSuccess);
    }

    [TestMethod]
    public void CategoryName_ValidInput_TrimsWhitespace()
    {
        // Arrange
        string raw = "  Category  ";
        // Act
        CategoryName name = Unwrap(CategoryName.Create(raw));
        // Assert
        Assert.AreEqual("Category", name.Value);
    }

    [TestMethod]
    public void Create_ValidInputs_IsActiveIsTrue()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("Cat"));
        // Act
        Category category = Unwrap(Category.Create(name.Value));
        // Assert
        Assert.IsTrue(category.IsActive);
    }

    [TestMethod]
    public void Create_ValidInputs_SlugDerivedFromName()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("My Cat"));
        // Act
        Category category = Unwrap(Category.Create(name.Value));
        // Assert
        Slug expected = Unwrap(Slug.Create(name.Value));
        Assert.AreEqual(expected.Value, category.Slug.Value);
    }

    [TestMethod]
    public void Create_ValidInputs_RaisesCategoryCreatedEvent()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        // Act
        Category category = Unwrap(Category.Create(name.Value));
        // Assert
        bool hasEvent = category.DomainEvents.OfType<CategoryCreatedEvent>().Any();
        Assert.IsTrue(hasEvent);
    }

    [TestMethod]
    public void Create_EmptyName_ReturnsFailureWithCategoryNameEmptyCode()
    {
        // Act
        var result = Category.Create("");
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATEGORY_NAME_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NameTooLong_ReturnsFailureWithCategoryNameTooLongCode()
    {
        // Arrange
        string raw = new string('a', 101);
        // Act
        var result = Category.Create(raw);
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATEGORY_NAME_TOO_LONG", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_WithParentId_ParentIdIsSet()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        Guid parent = Guid.NewGuid();
        // Act
        Category category = Unwrap(Category.Create(name.Value, parent));
        // Assert
        Assert.AreEqual(parent, category.ParentId);
    }

    [TestMethod]
    public void Create_WithoutParentId_ParentIdIsNull()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        // Act
        Category category = Unwrap(Category.Create(name.Value));
        // Assert
        Assert.IsNull(category.ParentId);
    }

    [TestMethod]
    public void Rename_NewName_SlugRegeneratedFromNewName()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("Old"));
        Category category = Unwrap(Category.Create(name.Value));
        CategoryName newName = Unwrap(CategoryName.Create("Brand New"));
        // Act
        category.Rename(newName);
        // Assert
        Slug expected = Unwrap(Slug.Create(newName.Value));
        Assert.AreEqual(expected.Value, category.Slug.Value);
    }

    [TestMethod]
    public void Rename_NewName_NameIsUpdated()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("Old"));
        Category category = Unwrap(Category.Create(name.Value));
        CategoryName newName = Unwrap(CategoryName.Create("Brand New"));
        // Act
        category.Rename(newName);
        // Assert
        Assert.AreEqual(newName.Value, category.Name.Value);
    }

    [TestMethod]
    public void Deactivate_ActiveCategory_IsActiveIsFalse()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        Category category = Unwrap(Category.Create(name.Value));
        // Act
        category.Deactivate();
        // Assert
        Assert.IsFalse(category.IsActive);
    }

    [TestMethod]
    public void MoveTo_DifferentParent_ParentIdUpdated()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        Category category = Unwrap(Category.Create(name.Value));
        Guid newParent = Guid.NewGuid();
        // Act
        category.MoveTo(newParent);
        // Assert
        Assert.AreEqual(newParent, category.ParentId);
    }

    [TestMethod]
    public void MoveTo_SameIdAsCategory_ReturnsFailure()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        Category category = Unwrap(Category.Create(name.Value));
        // Act & Assert
        var moveRes = category.MoveTo(category.Id);
        Assert.IsFalse(moveRes.IsSuccess);
    }

    [TestMethod]
    public void MoveTo_Null_ParentIdBecomesNull()
    {
        // Arrange
        CategoryName name = Unwrap(CategoryName.Create("C"));
        Guid parent = Guid.NewGuid();
        Category category = Unwrap(Category.Create(name.Value, parent));
        // Act
        category.MoveTo(null);
        // Assert
        Assert.IsNull(category.ParentId);
    }
}
