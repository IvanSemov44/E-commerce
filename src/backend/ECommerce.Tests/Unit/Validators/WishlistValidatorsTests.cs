using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;
using FluentValidation.TestHelper;

namespace ECommerce.Tests.Unit.Validators;

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
        var command = new RemoveFromWishlistCommand(Guid.Empty, Guid.NewGuid());

        var result = _removeValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void RemoveFromWishlist_Should_Have_Error_When_ProductId_Empty()
    {
        var command = new RemoveFromWishlistCommand(Guid.NewGuid(), Guid.Empty);

        var result = _removeValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void RemoveFromWishlist_Should_Pass_Valid_Command()
    {
        var command = new RemoveFromWishlistCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _removeValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void ClearWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var command = new ClearWishlistCommand(Guid.Empty);

        var result = _clearValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void ClearWishlist_Should_Pass_Valid_Command()
    {
        var command = new ClearWishlistCommand(Guid.NewGuid());

        var result = _clearValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void GetWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var query = new GetWishlistQuery(Guid.Empty);

        var result = _getValidator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void GetWishlist_Should_Pass_Valid_Query()
    {
        var query = new GetWishlistQuery(Guid.NewGuid());

        var result = _getValidator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Have_Error_When_UserId_Empty()
    {
        var query = new IsProductInWishlistQuery(Guid.Empty, Guid.NewGuid());

        var result = _isInWishlistValidator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Have_Error_When_ProductId_Empty()
    {
        var query = new IsProductInWishlistQuery(Guid.NewGuid(), Guid.Empty);

        var result = _isInWishlistValidator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [TestMethod]
    public void IsProductInWishlist_Should_Pass_Valid_Query()
    {
        var query = new IsProductInWishlistQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = _isInWishlistValidator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
