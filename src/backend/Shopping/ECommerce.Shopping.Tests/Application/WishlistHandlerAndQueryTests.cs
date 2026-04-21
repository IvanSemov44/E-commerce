using ECommerce.Shopping.Application.Commands.AddToWishlist;
using ECommerce.Shopping.Application.Commands.ClearWishlist;
using ECommerce.Shopping.Application.Commands.RemoveFromWishlist;
using ECommerce.Shopping.Application.Errors;
using ECommerce.Shopping.Application.Queries.GetWishlist;
using ECommerce.Shopping.Application.Queries.IsProductInWishlist;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class WishlistHandlerAndQueryTests
{
    [TestMethod]
    public async Task AddToWishlist_UnknownProduct_ReturnsProductNotFound()
    {
        var wishlists = new FakeWishlistRepository();
        var products = new FakeShoppingProductReader();
        var handler = new AddToWishlistCommandHandler(wishlists, products);

        var result = await handler.Handle(
            new AddToWishlistCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.GetErrorOrThrow().Code.ShouldBe(ShoppingApplicationErrors.ProductNotFound.Code);
    }

    [TestMethod]
    public async Task AddToWishlist_DuplicateProduct_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var wishlists = new FakeWishlistRepository();
        var products = new FakeShoppingProductReader();
        products.Products[productId] = (10m, "USD");
        var handler = new AddToWishlistCommandHandler(wishlists, products);

        var first = await handler.Handle(new AddToWishlistCommand(userId, productId), CancellationToken.None);
        var second = await handler.Handle(new AddToWishlistCommand(userId, productId), CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        var wishlist = wishlists.Store.Single(x => x.UserId == userId);
        wishlist.ProductIds.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task RemoveFromWishlist_NonExistingProduct_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var wishlists = new FakeWishlistRepository();
        wishlists.Store.Add(Wishlist.Create(userId));
        var handler = new RemoveFromWishlistCommandHandler(wishlists);

        var result = await handler.Handle(
            new RemoveFromWishlistCommand(userId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ClearWishlist_NoWishlist_ReturnsOk()
    {
        var handler = new ClearWishlistCommandHandler(new FakeWishlistRepository());

        var result = await handler.Handle(new ClearWishlistCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetWishlist_WhenMissing_ReturnsEmptyWishlistDto()
    {
        var userId = Guid.NewGuid();
        var handler = new GetWishlistQueryHandler(new FakeWishlistRepository());

        var result = await handler.Handle(new GetWishlistQuery(userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().UserId.ShouldBe(userId);
        result.GetDataOrThrow().ProductIds.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task IsProductInWishlist_WhenMissing_ReturnsFalse()
    {
        var handler = new IsProductInWishlistQueryHandler(new FakeWishlistRepository());

        var result = await handler.Handle(
            new IsProductInWishlistQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.GetDataOrThrow().ShouldBeFalse();
    }
}
