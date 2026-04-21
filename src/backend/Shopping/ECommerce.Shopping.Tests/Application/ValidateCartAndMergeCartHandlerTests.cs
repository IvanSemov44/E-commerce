using ECommerce.Shopping.Application.Commands.MergeCart;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Application.Queries.ValidateCart;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class ValidateCartQueryHandlerTests
{
    [TestMethod]
    public async Task CartMissing_ReturnsCartNotFound()
    {
        var carts = new FakeCartRepository();
        var stock = new ConfigurableStockReader();
        var handler = new ValidateCartQueryHandler(carts, stock);

        var result = await handler.Handle(
            new ValidateCartQuery(Guid.NewGuid(), Guid.NewGuid(), IsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ShoppingApplicationErrors.CartNotFound.Code);
    }

    [TestMethod]
    public async Task NonOwnerNonAdmin_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cart = Cart.Create(ownerId);
        cart.AddItem(Guid.NewGuid(), 1, 10m, "USD");

        var carts = new FakeCartRepository();
        carts.Store.Add(cart);
        var stock = new ConfigurableStockReader();
        var handler = new ValidateCartQueryHandler(carts, stock);

        var result = await handler.Handle(
            new ValidateCartQuery(cart.Id, otherUserId, IsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ShoppingApplicationErrors.Forbidden.Code);
    }

    [TestMethod]
    public async Task AdminBypassesOwnership_AndReturnsOkWhenStockAvailable()
    {
        var ownerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = Cart.Create(ownerId);
        cart.AddItem(productId, 2, 10m, "USD");

        var carts = new FakeCartRepository();
        carts.Store.Add(cart);
        var stock = new ConfigurableStockReader();
        stock.SetStock(productId, available: true);
        var handler = new ValidateCartQueryHandler(carts, stock);

        var result = await handler.Handle(
            new ValidateCartQuery(cart.Id, Guid.NewGuid(), IsAdmin: true),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task AnyOutOfStockItem_ReturnsInsufficientStock()
    {
        var ownerId = Guid.NewGuid();
        var inStockProduct = Guid.NewGuid();
        var outOfStockProduct = Guid.NewGuid();
        var cart = Cart.Create(ownerId);
        cart.AddItem(inStockProduct, 1, 10m, "USD");
        cart.AddItem(outOfStockProduct, 1, 10m, "USD");

        var carts = new FakeCartRepository();
        carts.Store.Add(cart);
        var stock = new ConfigurableStockReader();
        stock.SetStock(inStockProduct, available: true);
        stock.SetStock(outOfStockProduct, available: false);
        var handler = new ValidateCartQueryHandler(carts, stock);

        var result = await handler.Handle(
            new ValidateCartQuery(cart.Id, ownerId, IsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe("INSUFFICIENT_STOCK");
    }

    private sealed class ConfigurableStockReader : IStockAvailabilityReader
    {
        private readonly Dictionary<Guid, bool> _availability = new();

        public void SetStock(Guid productId, bool available) => _availability[productId] = available;

        public Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
            => Task.FromResult(_availability.TryGetValue(productId, out var value) && value);
    }
}

[TestClass]
public class MergeCartCommandHandlerTests
{
    [TestMethod]
    public async Task SessionCartMissing_ReturnsOkAndDoesNotCreateUserCart()
    {
        var carts = new FakeCartRepository();
        var handler = new MergeCartCommandHandler(carts);

        var result = await handler.Handle(
            new MergeCartCommand(Guid.NewGuid(), "missing-session"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        carts.Store.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EmptySessionCart_ReturnsOkAndDoesNotDelete()
    {
        var sessionId = "s-1";
        var sessionCart = Cart.CreateAnonymous(sessionId);
        var carts = new FakeCartRepository();
        carts.Store.Add(sessionCart);
        var handler = new MergeCartCommandHandler(carts);

        var result = await handler.Handle(
            new MergeCartCommand(Guid.NewGuid(), sessionId),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        carts.Store.ShouldContain(c => c.Id == sessionCart.Id);
    }

    [TestMethod]
    public async Task ValidMerge_MovesItemsAndDeletesSessionCart()
    {
        var userId = Guid.NewGuid();
        var sessionId = "s-2";
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        var sessionCart = Cart.CreateAnonymous(sessionId);
        sessionCart.AddItem(productA, 1, 10m, "USD");
        sessionCart.AddItem(productB, 2, 5m, "USD");

        var userCart = Cart.Create(userId);
        userCart.AddItem(productA, 3, 10m, "USD");

        var carts = new FakeCartRepository();
        carts.Store.Add(sessionCart);
        carts.Store.Add(userCart);
        var handler = new MergeCartCommandHandler(carts);

        var result = await handler.Handle(new MergeCartCommand(userId, sessionId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        carts.Store.ShouldNotContain(c => c.Id == sessionCart.Id);

        var mergedUserCart = carts.Store.Single(c => c.Id == userCart.Id);
        mergedUserCart.Items.Count.ShouldBe(2);
        mergedUserCart.Items.ShouldContain(i => i.ProductId == productA && i.Quantity == 4);
        mergedUserCart.Items.ShouldContain(i => i.ProductId == productB && i.Quantity == 2);
    }

    [TestMethod]
    public async Task MergeFailure_KeepsSessionCart()
    {
        var userId = Guid.NewGuid();
        var sessionId = "s-3";

        var userCart = Cart.Create(userId);
        for (var i = 0; i < 50; i++)
        {
            userCart.AddItem(Guid.NewGuid(), 1, 1m, "USD");
        }

        var sessionCart = Cart.CreateAnonymous(sessionId);
        sessionCart.AddItem(Guid.NewGuid(), 1, 1m, "USD");

        var carts = new FakeCartRepository();
        carts.Store.Add(userCart);
        carts.Store.Add(sessionCart);
        var handler = new MergeCartCommandHandler(carts);

        var result = await handler.Handle(new MergeCartCommand(userId, sessionId), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ShoppingErrors.CartFull.Code);
        carts.Store.ShouldContain(c => c.Id == sessionCart.Id);
    }
}
