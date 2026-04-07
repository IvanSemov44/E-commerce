using FluentValidation.TestHelper;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.Contracts.DTOs.Orders;
using ECommerce.Contracts.Validators.Orders;
using FluentAssertions;

namespace ECommerce.Tests.Unit.Validators;

/// <summary>
/// Unit tests for order-related validators.
/// Tests CreateOrderDtoValidator and UpdateOrderStatusDtoValidator.
/// </summary>
[TestClass]
public class OrderValidatorsTests
{
    private CreateOrderDtoValidator _createOrderValidator = null!;
    private UpdateOrderStatusDtoValidator _updateStatusValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _createOrderValidator = new CreateOrderDtoValidator();
        _updateStatusValidator = new UpdateOrderStatusDtoValidator();
    }

    #region CreateOrderDtoValidator Tests

    [TestMethod]
    public void CreateOrder_Should_Fail_When_Items_Empty()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>(),
            ShippingAddress = new AddressDto { City = "Test", StreetLine1 = "Test St", PostalCode = "12345", State = "CA", Country = "USA" }
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [TestMethod]
    public void CreateOrder_Should_Fail_When_Items_Null()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = null,
            ShippingAddress = new AddressDto { City = "Test", StreetLine1 = "Test St", PostalCode = "12345", State = "CA", Country = "USA" }
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [TestMethod]
    public void CreateOrder_Should_Fail_When_ShippingAddress_Null()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            },
            ShippingAddress = null
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress);
    }

    [TestMethod]
    public void CreateOrder_Should_Pass_With_Valid_Data()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 2 },
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            },
            ShippingAddress = new AddressDto
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "Springfield",
                State = "IL",
                PostalCode = "62701",
                Country = "USA"
            }
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateOrder_Should_Validate_Each_Item_With_ItemValidator()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = "", Quantity = 0 } // Invalid
            },
            ShippingAddress = new AddressDto { City = "Test", StreetLine1 = "Test St", PostalCode = "12345", State = "CA", Country = "USA" }
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.Errors.Count.Should().BeGreaterThan(0); // Will have item validation errors
    }

    [TestMethod]
    public void CreateOrder_Should_Fail_With_Invalid_Address()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            },
            ShippingAddress = new AddressDto { City = "", StreetLine1 = "", PostalCode = "", State = "", Country = "" } // Invalid
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.Errors.Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void CreateOrder_With_Multiple_Valid_Items()
    {
        // Arrange
        var dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 1 },
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 2 },
                new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 3 }
            },
            ShippingAddress = new AddressDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                StreetLine1 = "456 Oak Ave",
                City = "Shelbyville",
                State = "IL",
                PostalCode = "62702",
                Country = "USA"
            }
        };

        // Act
        var result = _createOrderValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UpdateOrderStatusDtoValidator Tests

    [TestMethod]
    public void UpdateOrderStatus_Should_Fail_When_Status_Empty()
    {
        // Arrange
        var dto = new UpdateOrderStatusDto { Status = "" };

        // Act
        var result = _updateStatusValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [TestMethod]
    public void UpdateOrderStatus_Should_Fail_With_Invalid_Status()
    {
        // Arrange
        var dto = new UpdateOrderStatusDto { Status = "invalid_status" };

        // Act
        var result = _updateStatusValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [TestMethod]
    [DataRow("pending")]
    [DataRow("confirmed")]
    [DataRow("processing")]
    [DataRow("shipped")]
    [DataRow("delivered")]
    [DataRow("cancelled")]
    [DataRow("refunded")]
    public void UpdateOrderStatus_Should_Pass_With_Valid_Status(string status)
    {
        // Arrange
        var dto = new UpdateOrderStatusDto { Status = status };

        // Act
        var result = _updateStatusValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateOrderStatus_Should_Accept_Status_In_Any_Case()
    {
        // Arrange
        var dto = new UpdateOrderStatusDto { Status = "PENDING" };

        // Act
        var result = _updateStatusValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateOrderStatus_Should_Accept_Mixed_Case_Status()
    {
        // Arrange
        var dto = new UpdateOrderStatusDto { Status = "Confirmed" };

        // Act
        var result = _updateStatusValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CreateOrderItemDtoValidator Tests

    [TestMethod]
    public void CreateOrderItem_Should_Fail_When_ProductId_Empty()
    {
        // Arrange
        var dto = new CreateOrderItemDto { ProductId = "", Quantity = 1 };

        // Act
        var validator = new CreateOrderItemDtoValidator();
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void CreateOrderItem_Should_Fail_When_Quantity_Zero()
    {
        // Arrange
        var dto = new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 0 };

        // Act
        var validator = new CreateOrderItemDtoValidator();
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [TestMethod]
    public void CreateOrderItem_Should_Fail_When_Quantity_Negative()
    {
        // Arrange
        var dto = new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = -5 };

        // Act
        var validator = new CreateOrderItemDtoValidator();
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [TestMethod]
    public void CreateOrderItem_Should_Pass_With_Valid_Data()
    {
        // Arrange
        var dto = new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 1 };

        // Act
        var validator = new CreateOrderItemDtoValidator();
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreateOrderItem_Should_Pass_With_Large_Quantity()
    {
        // Arrange
        var dto = new CreateOrderItemDto { ProductId = Guid.NewGuid().ToString(), Quantity = 9999 };

        // Act
        var validator = new CreateOrderItemDtoValidator();
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

