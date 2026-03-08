using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the WishlistRepository class.
/// Tests wishlist-specific repository operations.
/// </summary>
[TestClass]
public class WishlistRepositoryTests
{
    private AppDbContext _context = null!;
    private WishlistRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new WishlistRepository(_context);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@test.com", FirstName = "John", LastName = "Doe" },
            new() { Id = Guid.NewGuid(), Email = "user2@test.com", FirstName = "Jane", LastName = "Smith" },
            new() { Id = Guid.NewGuid(), Email = "user3@test.com", FirstName = "Bob", LastName = "Wilson" }
        };

        _context.Users.AddRange(users);

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
        _context.Categories.Add(category);

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Slug = "laptop",
                Sku = "LAPTOP-001",
                Price = 999.99m,
                StockQuantity = 10,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Slug = "phone",
                Sku = "PHONE-001",
                Price = 599.99m,
                StockQuantity = 20,
                CategoryId = category.Id,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tablet",
                Slug = "tablet",
                Sku = "TABLET-001",
                Price = 399.99m,
                StockQuantity = 15,
                CategoryId = category.Id,
                IsActive = true
            }
        };

        _context.Products.AddRange(products);

        // Create wishlist items
        var wishlistItems = new List<Wishlist>
        {
            new() { Id = Guid.NewGuid(), UserId = users[0].Id, ProductId = products[0].Id, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = Guid.NewGuid(), UserId = users[0].Id, ProductId = products[1].Id, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = Guid.NewGuid(), UserId = users[1].Id, ProductId = products[0].Id, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = users[1].Id, ProductId = products[2].Id, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _context.Wishlists.AddRange(wishlistItems);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_UserWithWishlist_ReturnsFirstItem()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user1.Id);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_UserWithNoWishlist_ReturnsNull()
    {
        // Arrange
        var user3 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");

        // Act
        var result = await _repository.GetByUserIdAsync(user3.Id);

        // Assert
        result.Should().BeNull();
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
        _context.ChangeTracker.Entries<Wishlist>().Should().Contain(e => e.Entity.Id == result!.Id);
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
        _context.ChangeTracker.Entries<Wishlist>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetByUserIdWithItemsAsync Tests

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_UserWithWishlist_ReturnsWithProduct()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(user1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Product.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_UserWithNoWishlist_ReturnsNull()
    {
        // Arrange
        var user3 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");

        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(user3.Id);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_WithTracking_TracksEntity()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(user1.Id, trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Wishlist>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(user1.Id, trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Wishlist>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region IsProductInWishlistAsync Tests

    [TestMethod]
    public async Task IsProductInWishlistAsync_ProductInWishlist_ReturnsTrue()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.IsProductInWishlistAsync(user1.Id, laptop.Id);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsProductInWishlistAsync_ProductNotInWishlist_ReturnsFalse()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");
        var tablet = await _context.Products.FirstAsync(p => p.Slug == "tablet");

        // Act
        var result = await _repository.IsProductInWishlistAsync(user1.Id, tablet.Id);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsProductInWishlistAsync_UserWithNoWishlist_ReturnsFalse()
    {
        // Arrange
        var user3 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.IsProductInWishlistAsync(user3.Id, laptop.Id);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsProductInWishlistAsync_NonExistingProduct_ReturnsFalse()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.IsProductInWishlistAsync(user1.Id, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetWishlistItemCountAsync Tests

    [TestMethod]
    public async Task GetWishlistItemCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetWishlistItemCountAsync(user1.Id);

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task GetWishlistItemCountAsync_UserWithNoWishlist_ReturnsZero()
    {
        // Arrange
        var user3 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");

        // Act
        var result = await _repository.GetWishlistItemCountAsync(user3.Id);

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task GetWishlistItemCountAsync_NonExistingUser_ReturnsZero()
    {
        // Act
        var result = await _repository.GetWishlistItemCountAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Base Repository Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingWishlist_ReturnsWishlist()
    {
        // Arrange
        var wishlist = await _context.Wishlists.FirstAsync();

        // Act
        var result = await _repository.GetByIdAsync(wishlist.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(wishlist.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingWishlist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllWishlistItems()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(4);
    }

    [TestMethod]
    public async Task AddAsync_AddsWishlistToDatabase()
    {
        // Arrange
        var user3 = await _context.Users.FirstAsync(u => u.Email == "user3@test.com");
        var laptop = await _context.Products.FirstAsync(p => p.Slug == "laptop");
        var newWishlist = new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = user3.Id,
            ProductId = laptop.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(newWishlist);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(newWishlist.Id);
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Delete_RemovesWishlistFromDatabase()
    {
        // Arrange
        var wishlist = await _context.Wishlists.FirstAsync();

        // Act
        _repository.Delete(wishlist);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(wishlist.Id);
        result.Should().BeNull();
    }

    #endregion
}
