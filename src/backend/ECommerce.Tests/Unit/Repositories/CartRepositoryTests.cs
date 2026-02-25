using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the CartRepository class.
/// Tests cart-specific repository operations including retrieval, item management, and calculations.
/// </summary>
[TestClass]
public class CartRepositoryTests
{
    private AppDbContext _context = null!;
    private CartRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new CartRepository(_context);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", FirstName = "John", LastName = "Doe" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", FirstName = "Jane", LastName = "Smith" };

        _context.Users.AddRange(user1, user2);

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
        _context.Categories.Add(category);

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Slug = "laptop",
            Sku = "LAPTOP-001",
            Price = 999.99m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsActive = true
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Phone",
            Slug = "phone",
            Sku = "PHONE-001",
            Price = 599.99m,
            StockQuantity = 20,
            CategoryId = category.Id,
            IsActive = true
        };

        _context.Products.AddRange(product1, product2);

        // Add product images
        var image1 = new ProductImage { Id = Guid.NewGuid(), ProductId = product1.Id, Url = "laptop.jpg", IsMain = true };
        var image2 = new ProductImage { Id = Guid.NewGuid(), ProductId = product2.Id, Url = "phone.jpg", IsMain = true };
        _context.ProductImages.AddRange(image1, image2);

        // Create carts
        var cart1 = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            CreatedAt = DateTime.UtcNow
        };
        var cart2 = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            CreatedAt = DateTime.UtcNow
        };
        var cart3 = new Cart
        {
            Id = Guid.NewGuid(),
            SessionId = "guest-session-123",
            CreatedAt = DateTime.UtcNow
        };

        _context.Carts.AddRange(cart1, cart2, cart3);

        // Add cart items
        var cartItems = new List<CartItem>
        {
            new() { Id = Guid.NewGuid(), CartId = cart1.Id, ProductId = product1.Id, Quantity = 2 },
            new() { Id = Guid.NewGuid(), CartId = cart1.Id, ProductId = product2.Id, Quantity = 1 },
            new() { Id = Guid.NewGuid(), CartId = cart2.Id, ProductId = product1.Id, Quantity = 1 },
            new() { Id = Guid.NewGuid(), CartId = cart3.Id, ProductId = product2.Id, Quantity = 3 }
        };

        _context.CartItems.AddRange(cartItems);
        _context.SaveChanges();
    }

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_ExistingUser_ReturnsCartWithItems()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_IncludesProductAndImages()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.First().Product.Should().NotBeNull();
        result.Items.First().Product.Images.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WithTracking_TracksEntity()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id, trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id, trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetBySessionIdAsync Tests

    [TestMethod]
    public async Task GetBySessionIdAsync_ExistingSession_ReturnsCartWithItems()
    {
        // Act
        var result = await _repository.GetBySessionIdAsync("guest-session-123");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.SessionId.Should().Be("guest-session-123");
    }

    [TestMethod]
    public async Task GetBySessionIdAsync_NonExistingSession_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySessionIdAsync("non-existent-session");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBySessionIdAsync_IncludesProductAndImages()
    {
        // Act
        var result = await _repository.GetBySessionIdAsync("guest-session-123");

        // Assert
        result.Should().NotBeNull();
        result!.Items.First().Product.Should().NotBeNull();
        result.Items.First().Product.Images.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetBySessionIdAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetBySessionIdAsync("guest-session-123", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetBySessionIdAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetBySessionIdAsync("guest-session-123", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetCartWithItemsAsync Tests

    [TestMethod]
    public async Task GetCartWithItemsAsync_ExistingCart_ReturnsCartWithItems()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync(c => c.SessionId == null);

        // Act
        var result = await _repository.GetCartWithItemsAsync(cart.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetCartWithItemsAsync_NonExistingCart_ReturnsNull()
    {
        // Act
        var result = await _repository.GetCartWithItemsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetCartWithItemsAsync_IncludesProductAndImages()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync(c => c.SessionId == null);

        // Act
        var result = await _repository.GetCartWithItemsAsync(cart.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.First().Product.Should().NotBeNull();
        result.Items.First().Product.Images.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetCartWithItemsAsync_WithTracking_TracksEntity()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync(c => c.SessionId == null);

        // Act
        var result = await _repository.GetCartWithItemsAsync(cart.Id, trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetCartWithItemsAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync(c => c.SessionId == null);

        // Act
        var result = await _repository.GetCartWithItemsAsync(cart.Id, trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Cart>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region CartExistsForUserAsync Tests

    [TestMethod]
    public async Task CartExistsForUserAsync_ExistingCart_ReturnsTrue()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.CartExistsForUserAsync(user1.Id);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task CartExistsForUserAsync_NonExistingCart_ReturnsFalse()
    {
        // Act
        var result = await _repository.CartExistsForUserAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CalculateTotalAsync Tests

    [TestMethod]
    public async Task CalculateTotalAsync_ReturnsCorrectTotal()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
        var cart = await _context.Carts.FirstAsync(c => c.UserId == user1.Id);
        // Cart has: 2 laptops (999.99 * 2) + 1 phone (599.99) = 2599.97

        // Act
        var result = await _repository.CalculateTotalAsync(cart.Id);

        // Assert
        result.Should().BeApproximately(2599.97m, 0.01m);
    }

    [TestMethod]
    public async Task CalculateTotalAsync_NonExistingCart_ReturnsZero()
    {
        // Act
        var result = await _repository.CalculateTotalAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task CalculateTotalAsync_EmptyCart_ReturnsZero()
    {
        // Arrange
        var emptyCart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(emptyCart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CalculateTotalAsync(emptyCart.Id);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetCartItemCountAsync Tests

    [TestMethod]
    public async Task GetCartItemCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
        var cart = await _context.Carts.FirstAsync(c => c.UserId == user1.Id);
        // Cart has 2 different items (not quantities)

        // Act
        var result = await _repository.GetCartItemCountAsync(cart.Id);

        // Assert
        result.Should().Be(2); // Number of distinct items
    }

    [TestMethod]
    public async Task GetCartItemCountAsync_NonExistingCart_ReturnsZero()
    {
        // Act
        var result = await _repository.GetCartItemCountAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task GetCartItemCountAsync_EmptyCart_ReturnsZero()
    {
        // Arrange
        var emptyCart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(emptyCart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCartItemCountAsync(emptyCart.Id);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Base Repository Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingCart_ReturnsCart()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync();

        // Act
        var result = await _repository.GetByIdAsync(cart.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(cart.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingCart_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllCarts()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task AddAsync_AddsCartToDatabase()
    {
        // Arrange
        var newCart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(newCart);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(newCart.Id);
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Delete_RemovesCartFromDatabase()
    {
        // Arrange
        var cart = await _context.Carts.FirstAsync();

        // Act
        _repository.Delete(cart);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(cart.Id);
        result.Should().BeNull();
    }

    #endregion
}