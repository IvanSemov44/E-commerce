using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Application.DTOs;
using ECommerce.Shopping.Application.Queries.GetCart;
using ECommerce.Shopping.Tests.Application;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.SharedKernel.Results;
using Shouldly;

namespace ECommerce.Shopping.Tests.Application;

[TestClass]
public class GetCartQueryHandlerTests
{
    [TestClass]
    public class Handle
    {
        private static (FakeCartRepository repo, GetCartQueryHandler handler) Build()
        {
            var repo = new FakeCartRepository();
            return (repo, new GetCartQueryHandler(repo));
        }

        [TestMethod]
        public async Task NewUser_ReturnsEmptyCart()
        {
            var userId = Guid.NewGuid();
            var (repo, handler) = Build();

            var result = await handler.Handle(new GetCartQuery(userId, null), CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.GetDataOrThrow().Items.ShouldBeEmpty();
        }

        [TestMethod]
        public async Task ExistingUser_ReturnsCartWithItems()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var cart = Cart.Create(userId);
            cart.AddItem(productId, 2, 9.99m, "USD");
            var (repo, handler) = Build();
            repo.Store.Add(cart);

            var result = await handler.Handle(new GetCartQuery(userId, null), CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.GetDataOrThrow().Items.Count.ShouldBe(1);
        }
    }
}

[TestClass]
public class AddToCartCommandHandlerTests
{
    private static (FakeCartRepository cartRepo, FakeShoppingProductReader dbReader, AddToCartCommandHandler handler) Build()
    {
        var cartRepo = new FakeCartRepository();
        var dbReader = new FakeShoppingProductReader();
        return (cartRepo, dbReader, new AddToCartCommandHandler(cartRepo, dbReader));
    }

    [TestClass]
    public class Handle
    {
        [TestMethod]
        public async Task ValidProduct_AddsItem()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var cart = Cart.Create(userId);
            var (cartRepo, dbReader, handler) = Build();
            cartRepo.Store.Add(cart);
            dbReader.Products[productId] = (9.99m, "USD");

            var result = await handler.Handle(
                new AddToCartCommand(userId, null, productId, 2),
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            cartRepo.Store.First().Items.Count.ShouldBe(1);
        }

        [TestMethod]
        public async Task UnknownProduct_ReturnsError()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var cart = Cart.Create(userId);
            var (cartRepo, dbReader, handler) = Build();
            cartRepo.Store.Add(cart);

            var result = await handler.Handle(
                new AddToCartCommand(userId, null, productId, 2),
                CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.GetErrorOrThrow().Code.ShouldBe("PRODUCT_NOT_FOUND");
        }
    }
}
