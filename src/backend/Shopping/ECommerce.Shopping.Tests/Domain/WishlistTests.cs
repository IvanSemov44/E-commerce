using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Tests.Domain;

[TestClass]
public class WishlistTests
{
    private static readonly Guid _testUserId = Guid.NewGuid();
    private static readonly Guid _testProduct1 = Guid.NewGuid();
    private static readonly Guid _testProduct2 = Guid.NewGuid();

    [TestMethod]
    public void Create_Succeeds_WithUserId()
    {
        var wishlist = Wishlist.Create(_testUserId);

        Assert.AreEqual(_testUserId, wishlist.UserId);
        Assert.IsEmpty(wishlist.ProductIds);
    }

    [TestMethod]
    public void AddProduct_NewProduct_Adds()
    {
        var wishlist = Wishlist.Create(_testUserId);

        var result = wishlist.AddProduct(_testProduct1);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, wishlist.ProductIds);
        Assert.IsTrue(wishlist.Contains(_testProduct1));
    }

    [TestMethod]
    public void AddProduct_ExistingProduct_IsIdempotent()
    {
        var wishlist = Wishlist.Create(_testUserId);
        wishlist.AddProduct(_testProduct1);

        var result = wishlist.AddProduct(_testProduct1);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, wishlist.ProductIds);
    }

    [TestMethod]
    public void AddProduct_Exceeds100_ReturnsWishlistFull()
    {
        var wishlist = Wishlist.Create(_testUserId);
        for (int i = 0; i < 100; i++)
        {
            wishlist.AddProduct(Guid.NewGuid());
        }

        var result = wishlist.AddProduct(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ShoppingErrors.WishlistFull, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void RemoveProduct_Existing_Removes()
    {
        var wishlist = Wishlist.Create(_testUserId);
        wishlist.AddProduct(_testProduct1);

        wishlist.RemoveProduct(_testProduct1);

        Assert.IsEmpty(wishlist.ProductIds);
        Assert.IsFalse(wishlist.Contains(_testProduct1));
    }

    [TestMethod]
    public void RemoveProduct_NonExisting_IsNoOp()
    {
        var wishlist = Wishlist.Create(_testUserId);

        wishlist.RemoveProduct(_testProduct1);

        Assert.IsEmpty(wishlist.ProductIds);
    }

    [TestMethod]
    public void Contains_Existing_ReturnsTrue()
    {
        var wishlist = Wishlist.Create(_testUserId);
        wishlist.AddProduct(_testProduct1);

        Assert.IsTrue(wishlist.Contains(_testProduct1));
    }

    [TestMethod]
    public void Contains_NonExisting_ReturnsFalse()
    {
        var wishlist = Wishlist.Create(_testUserId);

        Assert.IsFalse(wishlist.Contains(_testProduct1));
    }

    [TestMethod]
    public void Clear_RemovesAll()
    {
        var wishlist = Wishlist.Create(_testUserId);
        wishlist.AddProduct(_testProduct1);
        wishlist.AddProduct(_testProduct2);

        wishlist.Clear();

        Assert.IsEmpty(wishlist.ProductIds);
    }
}
