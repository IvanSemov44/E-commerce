using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.Shopping.Tests.Application;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class GetCartQueryHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [TestMethod]
    public async Task Handle_GetCart_NewUser_ReturnsEmptyCart()
    {
        var repo = new FakeCartRepository();
        var handler = new GetCartQueryHandler(repo);

        var result = await handler.Handle(new GetCartQuery(_userId, null), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        if (result is Result<CartDto>.Success success)
            Assert.IsEmpty(success.Data.Items);
    }

    [TestMethod]
    public async Task Handle_GetCart_ExistingUser_ReturnsCart()
    {
        var repo = new FakeCartRepository();
        var cart = Cart.Create(_userId);
        cart.AddItem(_productId, 2, 9.99m, "USD");
        repo.Store.Add(cart);
        var handler = new GetCartQueryHandler(repo);

        var result = await handler.Handle(new GetCartQuery(_userId, null), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        if (result is Result<CartDto>.Success success)
            Assert.HasCount(1, success.Data.Items);
    }
}

[TestClass]
public class AddToCartCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [TestMethod]
    public async Task Handle_AddToCart_ValidProduct_AddsItem()
    {
        var cartRepo = new FakeCartRepository();
        var dbReader = new FakeShoppingDbReader();
        dbReader.Products[_productId] = (9.99m, "USD");
        var uow = new FakeUnitOfWork();
        var handler = new AddToCartCommandHandler(cartRepo, dbReader, uow);

        var result = await handler.Handle(
            new AddToCartCommand(_userId, null, _productId, 2),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task Handle_AddToCart_UnknownProduct_ReturnsError()
    {
        var cartRepo = new FakeCartRepository();
        var dbReader = new FakeShoppingDbReader();
        var uow = new FakeUnitOfWork();
        var handler = new AddToCartCommandHandler(cartRepo, dbReader, uow);

        var result = await handler.Handle(
            new AddToCartCommand(_userId, null, _productId, 2),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
    }
}
