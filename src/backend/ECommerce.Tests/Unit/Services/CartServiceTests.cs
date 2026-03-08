using AutoMapper;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class CartServiceTests
{
    private Mock<ICartRepository> _mockCartRepository = null!;
    private Mock<IRepository<CartItem>> _mockCartItemRepository = null!;
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ILogger<CartService>> _mockLogger = null!;
    private CartService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockCartRepository = new Mock<ICartRepository>();
        _mockCartItemRepository = MockHelpers.CreateMockRepository<CartItem>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLogger = new Mock<ILogger<CartService>>();

        _mockUnitOfWork.Setup(u => u.Carts).Returns(_mockCartRepository.Object);
        _mockUnitOfWork.Setup(u => u.CartItems).Returns(_mockCartItemRepository.Object);
        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);

        _service = new CartService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);

        // Sanity check: ensure unit of work is wired to product repo
        _mockUnitOfWork.Object.Products.Should().BeSameAs(_mockProductRepository.Object);
    }

    #region GetOrCreateCartAsync Tests

    [TestMethod]
    public async Task GetOrCreateCartAsync_NewUserCart_CreatesCart()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((Cart?)null);

        _mockCartRepository.Setup(r => r.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, _) => { if (cart.Id == Guid.Empty) cart.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.GetOrCreateCartAsync(userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CartDto>.Success success)
        {
            success.Data.Id.Should().NotBeEmpty();
        }
        _mockCartRepository.Verify(r => r.AddAsync(It.Is<Cart>(c => c.UserId == userId)), Times.Once);
    }

    [TestMethod]
    public async Task GetOrCreateCartAsync_NewSessionCart_CreatesCart()
    {
        // Arrange
        var sessionId = "session-123";

        _mockCartRepository.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync((Cart?)null);

        _mockCartRepository.Setup(r => r.AddAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, _) => { if (cart.Id == Guid.Empty) cart.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.GetOrCreateCartAsync(null, sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CartDto>.Success success)
        {
            success.Data.Id.Should().NotBeEmpty();
        }
        _mockCartRepository.Verify(r => r.AddAsync(It.Is<Cart>(c => c.SessionId == sessionId)), Times.Once);
    }

    [TestMethod]
    public async Task GetOrCreateCartAsync_ExistingCart_ReturnsCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.GetOrCreateCartAsync(userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCartRepository.Verify(r => r.AddAsync(It.IsAny<Cart>()), Times.Never);
    }

    #endregion

    #region GetCartAsync Tests

    [TestMethod]
    public async Task GetCartAsync_ExistingCart_ReturnsCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.GetCartAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CartDto>.Success success)
        {
            success.Data.Id.Should().Be(cart.Id);
        }
    }

    [TestMethod]
    public async Task GetCartAsync_CartNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((Cart?)null);

        // Act
        var result = await _service.GetCartAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CartNotFound);
        }
    }

    #endregion

    #region AddToCartAsync Tests

    [TestMethod]
    public async Task AddToCartAsync_NewItem_AddsToCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 10, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => id == product.Id ? product : null);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.AddToCartAsync(userId, null, product.Id, 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCartItemRepository.Verify(r => r.AddAsync(It.Is<CartItem>(
            ci => ci.ProductId == product.Id && ci.Quantity == 2), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AddToCartAsync_ExistingItem_IncrementsQuantity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 10, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);
        var existingCartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 2);
        cart.Items.Add(existingCartItem);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => id == product.Id ? product : null);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.AddToCartAsync(userId, null, product.Id, 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCartItem.Quantity.Should().Be(5); // 2 + 3
        _mockCartItemRepository.Verify(r => r.AddAsync(It.IsAny<CartItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AddToCartAsync_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 5, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => id == product.Id ? product : null);

        // Act
        var result = await _service.AddToCartAsync(userId, null, product.Id, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InsufficientStock);
        }
    }

    [TestMethod]
    public async Task AddToCartAsync_InvalidQuantity_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var result = await _service.AddToCartAsync(userId, null, productId, 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InvalidQuantity);
        }
    }

    [TestMethod]
    public async Task AddToCartAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.AddToCartAsync(userId, null, productId, 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ProductNotFound);
        }
    }

    #endregion

    #region UpdateCartItemAsync Tests

    [TestMethod]
    public async Task UpdateCartItemAsync_ValidQuantity_UpdatesItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 10, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 2);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => id == product.Id ? product : null);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.UpdateCartItemAsync(userId, null, cartItem.Id, 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cartItem.Quantity.Should().Be(5);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_QuantityZero_RemovesItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 10, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 2);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => id == product.Id ? product : null);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.UpdateCartItemAsync(userId, null, cartItem.Id, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().NotContain(cartItem);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 5, price: 100);
        var cart = TestDataFactory.CreateCart(userId: userId);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 2);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockProductRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.UpdateCartItemAsync(userId, null, cartItem.Id, 10);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InsufficientStock);
        }
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_NegativeQuantity_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();

        // Act
        var result = await _service.UpdateCartItemAsync(userId, null, cartItemId, -1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InvalidQuantity);
        }
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);
        var nonExistentItemId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.UpdateCartItemAsync(userId, null, nonExistentItemId, 5);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CartItemNotFound);
        }
    }

    #endregion

    #region RemoveFromCartAsync Tests

    [TestMethod]
    public async Task RemoveFromCartAsync_ExistingItem_RemovesItem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct();
        var cart = TestDataFactory.CreateCart(userId: userId);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 2);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.RemoveFromCartAsync(userId, null, cartItem.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCartItemRepository.Verify(r => r.DeleteAsync(cartItem, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RemoveFromCartAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);
        var nonExistentItemId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.RemoveFromCartAsync(userId, null, nonExistentItemId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CartItemNotFound);
        }
    }

    #endregion

    #region ClearCartAsync Tests

    [TestMethod]
    public async Task ClearCartAsync_WithItems_ClearsAllItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = TestDataFactory.CreateCart(userId: userId);
        var product1 = TestDataFactory.CreateProduct();
        var product2 = TestDataFactory.CreateProduct();
        var cartItem1 = TestDataFactory.CreateCartItem(cart.Id, product1.Id, quantity: 2);
        var cartItem2 = TestDataFactory.CreateCartItem(cart.Id, product2.Id, quantity: 3);
        cart.Items.Add(cartItem1);
        cart.Items.Add(cartItem2);

        _mockCartRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(cart);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.ClearCartAsync(userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCartItemRepository.Verify(r => r.DeleteRangeAsync(It.IsAny<List<CartItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetCartByIdAsync Tests

    [TestMethod]
    public async Task GetCartByIdAsync_ExistingCart_ReturnsCart()
    {
        // Arrange
        var cart = TestDataFactory.CreateCart();

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.GetCartByIdAsync(cart.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CartDto>.Success success)
        {
            success.Data.Id.Should().Be(cart.Id);
        }
    }

    [TestMethod]
    public async Task GetCartByIdAsync_CartNotFound_ReturnsFailure()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cartId))
            .ReturnsAsync((Cart?)null);

        // Act
        var result = await _service.GetCartByIdAsync(cartId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CartDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CartNotFound);
        }
    }

    #endregion

    #region ValidateCartAsync Tests

    [TestMethod]
    public async Task ValidateCartAsync_ValidCart_CompletesSuccessfully()
    {
        // Arrange
        var cart = TestDataFactory.CreateCart();
        var product = TestDataFactory.CreateProduct(stock: 10, isActive: true);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 5);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        var products = new List<Product> { product }.AsAsyncQueryable();
        _mockProductRepository.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<bool>()))
            .Returns(products);

        // Act
        var result = await _service.ValidateCartAsync(cart.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task ValidateCartAsync_CartNotFound_ReturnsFailure()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cartId))
            .ReturnsAsync((Cart?)null);

        // Act
        var result = await _service.ValidateCartAsync(cartId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CartNotFound);
        }
    }

    [TestMethod]
    public async Task ValidateCartAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var cart = TestDataFactory.CreateCart();
        var productId = Guid.NewGuid();
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, productId, quantity: 5);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        var products = new List<Product>().AsAsyncQueryable();
        _mockProductRepository.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<bool>()))
            .Returns(products);

        // Act
        var result = await _service.ValidateCartAsync(cart.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ProductNotFound);
        }
    }

    [TestMethod]
    public async Task ValidateCartAsync_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var cart = TestDataFactory.CreateCart();
        var product = TestDataFactory.CreateProduct(stock: 3, isActive: true);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 5);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        var products = new List<Product> { product }.AsAsyncQueryable();
        _mockProductRepository.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<bool>()))
            .Returns(products);

        // Act
        var result = await _service.ValidateCartAsync(cart.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.InsufficientStock);
        }
    }

    [TestMethod]
    public async Task ValidateCartAsync_InactiveProduct_ReturnsFailure()
    {
        // Arrange
        var cart = TestDataFactory.CreateCart();
        var product = TestDataFactory.CreateProduct(stock: 10, isActive: false);
        var cartItem = TestDataFactory.CreateCartItem(cart.Id, product.Id, quantity: 5);
        cart.Items.Add(cartItem);

        _mockCartRepository.Setup(r => r.GetCartWithItemsAsync(cart.Id))
            .ReturnsAsync(cart);

        var products = new List<Product> { product }.AsAsyncQueryable();
        _mockProductRepository.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<bool>()))
            .Returns(products);

        // Act
        var result = await _service.ValidateCartAsync(cart.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.ProductNotAvailable);
        }
    }

    #endregion
}
