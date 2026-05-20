using FluentValidation.TestHelper;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;

namespace ECommerce.Shopping.Tests.Validators;

[TestClass]
public class WishlistValidatorsTests
{
    private RemoveFromWishlistCommandValidator _removeValidator = null!;
    private ClearWishlistCommandValidator _clearValidator = null!;
    private GetWishlistQueryValidator _getValidator = null!;
    private IsProductInWishlistQueryValidator _isInWishlistValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _removeValidator = new RemoveFromWishlistCommandValidator();
        _clearValidator = new ClearWishlistCommandValidator();
        _getValidator = new GetWishlistQueryValidator();
        _isInWishlistValidator = new IsProductInWishlistQueryValidator();
    }

    [TestMethod]
    public void RemoveFromWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var result = _removeValidator.TestValidate(new RemoveFromWishlistCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void RemoveFromWishlist_Should_Have_Error_When_ProductId_Empty()
    {
        var result = _removeValidator.TestValidate(new RemoveFromWishlistCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void RemoveFromWishlist_Should_Pass_Valid_Command()
    {
        var result = _removeValidator.TestValidate(new RemoveFromWishlistCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void ClearWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var result = _clearValidator.TestValidate(new ClearWishlistCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void ClearWishlist_Should_Pass_Valid_Command()
    {
        var result = _clearValidator.TestValidate(new ClearWishlistCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void GetWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var result = _getValidator.TestValidate(new GetWishlistQuery(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void GetWishlist_Should_Pass_Valid_Query()
    {
        var result = _getValidator.TestValidate(new GetWishlistQuery(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var result = _isInWishlistValidator.TestValidate(new IsProductInWishlistQuery(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Have_Error_When_ProductId_Empty()
    {
        var result = _isInWishlistValidator.TestValidate(new IsProductInWishlistQuery(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Pass_Valid_Query()
    {
        var result = _isInWishlistValidator.TestValidate(new IsProductInWishlistQuery(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
