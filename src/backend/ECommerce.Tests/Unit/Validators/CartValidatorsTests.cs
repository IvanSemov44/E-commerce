using FluentValidation.TestHelper;
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Validators;

[TestClass]
public class CartValidatorsTests
{
    private AddToCartCommandValidator _addValidator = null!;
    private UpdateCartItemQuantityCommandValidator _updateValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _addValidator = new AddToCartCommandValidator();
        _updateValidator = new UpdateCartItemQuantityCommandValidator();
    }

    [TestMethod]
    public void AddToCart_Should_Have_Error_When_Quantity_Invalid()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), "session", Guid.NewGuid(), 0);
        var result = _addValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [TestMethod]
    public void AddToCart_Should_Pass_Valid_Command()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), "session", Guid.NewGuid(), 2);
        var result = _addValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void UpdateCartItem_Should_Have_Error_When_NewQuantity_Invalid()
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), "session", Guid.NewGuid(), 0);
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewQuantity);
    }

    [TestMethod]
    public void UpdateCartItem_Should_Pass_Valid_Command()
    {
        var command = new UpdateCartItemQuantityCommand(Guid.NewGuid(), "session", Guid.NewGuid(), 3);
        var result = _updateValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

