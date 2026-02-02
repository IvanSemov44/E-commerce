using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Validators.Products;
using FluentAssertions;

namespace ECommerce.Tests.Unit.Validators;

/// <summary>
/// Unit tests for product-related validators.
/// Tests CreateProductDtoValidator and UpdateProductDtoValidator.
/// </summary>
[TestClass]
public class ProductValidatorsTests
{
    private CreateProductDtoValidator _createValidator = null!;
    private UpdateProductDtoValidator _updateValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _createValidator = new CreateProductDtoValidator();
        _updateValidator = new UpdateProductDtoValidator();
    }

    #region CreateProductDtoValidator Tests

    [TestMethod]
    public void CreateProduct_Should_Fail_When_Name_Empty()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "", Slug = "test", Price = 10.0M, StockQuantity = 5, CategoryId = Guid.NewGuid() };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = new string('a', 201),
            Slug = "test",
            Price = 10.0M,
            StockQuantity = 5,
            CategoryId = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_Slug_Empty()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test Product", Slug = "", Price = 10.0M, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_Price_Zero()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Slug = "test", Price = 0, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_Price_Negative()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Slug = "test", Price = -5.0M, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_StockQuantity_Negative()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Test", Slug = "test", Price = 10.0M, StockQuantity = -1, CategoryId = null };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [TestMethod]
    public void CreateProduct_Should_Pass_With_Valid_Data()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Excellent Product",
            Slug = "excellent-product",
            Price = 99.99M,
            StockQuantity = 100,
            CategoryId = Guid.NewGuid()
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateProduct_Should_Pass_With_Zero_Stock()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Pre-Order Product",
            Slug = "preorder-product",
            Price = 49.99M,
            StockQuantity = 0,
            CategoryId = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateProduct_Should_Fail_When_CompareAtPrice_LessThanOrEqual_Price()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Sale Product",
            Slug = "sale-product",
            Price = 100.0M,
            CompareAtPrice = 100.0M, // Not greater than price
            StockQuantity = 50,
            CategoryId = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    [TestMethod]
    public void CreateProduct_Should_Pass_When_CompareAtPrice_Greater_Than_Price()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Sale Product",
            Slug = "sale-product",
            Price = 79.99M,
            CompareAtPrice = 99.99M,
            StockQuantity = 50,
            CategoryId = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateProduct_Should_Pass_With_Null_CompareAtPrice()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Regular Product",
            Slug = "regular-product",
            Price = 50.0M,
            CompareAtPrice = null,
            StockQuantity = 200,
            CategoryId = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UpdateProductDtoValidator Tests

    [TestMethod]
    public void UpdateProduct_Should_Fail_When_Name_Empty()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "", Slug = "test", Price = 10.0M, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestMethod]
    public void UpdateProduct_Should_Fail_When_Slug_Empty()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Slug = "", Price = 10.0M, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [TestMethod]
    public void UpdateProduct_Should_Fail_When_Price_Zero()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Slug = "test", Price = 0, StockQuantity = 5, CategoryId = null };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestMethod]
    public void UpdateProduct_Should_Fail_When_StockQuantity_Negative()
    {
        // Arrange
        var dto = new UpdateProductDto { Name = "Test", Slug = "test", Price = 10.0M, StockQuantity = -1, CategoryId = null };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [TestMethod]
    public void UpdateProduct_Should_Pass_With_Valid_Data()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            Name = "Updated Product",
            Slug = "updated-product",
            Price = 79.99M,
            StockQuantity = 75,
            CategoryId = null
        };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateProduct_Should_Fail_When_CompareAtPrice_LessThanOrEqual_Price()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            Name = "Sale Product",
            Slug = "sale-product",
            Price = 100.0M,
            CompareAtPrice = 99.0M, // Less than price
            StockQuantity = 50,
            CategoryId = null
        };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    [TestMethod]
    public void UpdateProduct_Should_Pass_When_CompareAtPrice_Greater_Than_Price()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            Name = "Discounted Product",
            Slug = "discounted-product",
            Price = 49.99M,
            CompareAtPrice = 79.99M,
            StockQuantity = 100,
            CategoryId = null
        };

        // Act
        var result = _updateValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateProduct_Should_Allow_Multiple_Valid_Updates()
    {
        // Arrange - multiple updates
        var dto1 = new UpdateProductDto { Name = "Product 1", Slug = "product-1", Price = 10M, StockQuantity = 5, CategoryId = null };
        var dto2 = new UpdateProductDto { Name = "Product 2", Slug = "product-2", Price = 20M, StockQuantity = 10, CategoryId = null };

        // Act
        var result1 = _updateValidator.TestValidate(dto1);
        var result2 = _updateValidator.TestValidate(dto2);

        // Assert
        result1.ShouldNotHaveAnyValidationErrors();
        result2.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
