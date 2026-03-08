using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Validators.Cart;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Validators;

[TestClass]
public class CartValidatorsTests
{
    private AddToCartDtoValidator _addValidator = null!;
    private UpdateCartItemDtoValidator _updateValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _addValidator = new AddToCartDtoValidator();
        _updateValidator = new UpdateCartItemDtoValidator();
    }

    [TestMethod]
    public void AddToCart_Should_Have_Error_When_ProductId_Empty()
    {
        var dto = new AddToCartDto { ProductId = Guid.Empty, Quantity = 1 };
        var result = _addValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void AddToCart_Should_Have_Error_When_Quantity_Invalid()
    {
        var dto = new AddToCartDto { ProductId = Guid.NewGuid(), Quantity = 0 };
        var result = _addValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [TestMethod]
    public void AddToCart_Should_Pass_Valid_Dto()
    {
        var dto = new AddToCartDto { ProductId = Guid.NewGuid(), Quantity = 2 };
        var result = _addValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateCartItem_Should_Have_Error_When_Quantity_Invalid()
    {
        var dto = new UpdateCartItemDto { Quantity = 0 };
        var result = _updateValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [TestMethod]
    public void UpdateCartItem_Should_Pass_Valid_Dto()
    {
        var dto = new UpdateCartItemDto { Quantity = 3 };
        var result = _updateValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
