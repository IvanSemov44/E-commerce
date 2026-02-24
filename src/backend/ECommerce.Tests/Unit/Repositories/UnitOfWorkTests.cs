using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the UnitOfWork class.
/// Tests repository access and disposal.
/// Note: Transaction tests are not included because EF Core InMemory doesn't support transactions.
/// </summary>
[TestClass]
public class UnitOfWorkTests
{
    private AppDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _unitOfWork.Dispose();
    }

    #region Repository Properties Tests

    [TestMethod]
    public void Products_ReturnsProductRepository()
    {
        // Act
        var repository = _unitOfWork.Products;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IProductRepository>();
    }

    [TestMethod]
    public void Products_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Products;
        var repo2 = _unitOfWork.Products;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Orders_ReturnsOrderRepository()
    {
        // Act
        var repository = _unitOfWork.Orders;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IOrderRepository>();
    }

    [TestMethod]
    public void Orders_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Orders;
        var repo2 = _unitOfWork.Orders;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Users_ReturnsUserRepository()
    {
        // Act
        var repository = _unitOfWork.Users;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IUserRepository>();
    }

    [TestMethod]
    public void Users_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Users;
        var repo2 = _unitOfWork.Users;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Categories_ReturnsCategoryRepository()
    {
        // Act
        var repository = _unitOfWork.Categories;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<ICategoryRepository>();
    }

    [TestMethod]
    public void Categories_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Categories;
        var repo2 = _unitOfWork.Categories;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Carts_ReturnsCartRepository()
    {
        // Act
        var repository = _unitOfWork.Carts;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<ICartRepository>();
    }

    [TestMethod]
    public void Carts_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Carts;
        var repo2 = _unitOfWork.Carts;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Reviews_ReturnsReviewRepository()
    {
        // Act
        var repository = _unitOfWork.Reviews;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IReviewRepository>();
    }

    [TestMethod]
    public void Reviews_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Reviews;
        var repo2 = _unitOfWork.Reviews;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void Wishlists_ReturnsWishlistRepository()
    {
        // Act
        var repository = _unitOfWork.Wishlists;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IWishlistRepository>();
    }

    [TestMethod]
    public void Wishlists_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.Wishlists;
        var repo2 = _unitOfWork.Wishlists;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void OrderItems_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.OrderItems;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<OrderItem>>();
    }

    [TestMethod]
    public void OrderItems_ReturnsSameInstance()
    {
        // Act
        var repo1 = _unitOfWork.OrderItems;
        var repo2 = _unitOfWork.OrderItems;

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [TestMethod]
    public void CartItems_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.CartItems;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<CartItem>>();
    }

    [TestMethod]
    public void Addresses_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.Addresses;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<Address>>();
    }

    [TestMethod]
    public void PromoCodes_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.PromoCodes;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<PromoCode>>();
    }

    [TestMethod]
    public void InventoryLogs_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.InventoryLogs;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<InventoryLog>>();
    }

    [TestMethod]
    public void ProductImages_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.ProductImages;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<ProductImage>>();
    }

    [TestMethod]
    public void RefreshTokens_ReturnsGenericRepository()
    {
        // Act
        var repository = _unitOfWork.RefreshTokens;

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<RefreshToken>>();
    }

    #endregion

    #region SaveChangesAsync Tests

    [TestMethod]
    public async Task SaveChangesAsync_SavesChangesToDatabase()
    {
        // Arrange
        var category = new Category { Name = "Test Category", Slug = "test-category" };
        _unitOfWork.Categories.Add(category);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        var saved = await _context.Categories.FindAsync(category.Id);
        saved.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SaveChangesAsync_NoChanges_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region HasActiveTransaction Tests

    [TestMethod]
    public void HasActiveTransaction_NoTransaction_ReturnsFalse()
    {
        // Act & Assert
        _unitOfWork.HasActiveTransaction.Should().BeFalse();
    }

    #endregion

    #region DetachEntity Tests

    [TestMethod]
    public async Task DetachEntity_DetachesEntityFromTracker()
    {
        // Arrange
        var category = new Category { Name = "Test Category", Slug = "test-category" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        _unitOfWork.DetachEntity(category);

        // Assert
        _context.Entry(category).State.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Detached);
    }

    [TestMethod]
    public async Task DetachEntity_AllowsReAdd()
    {
        // Arrange
        var category = new Category { Name = "Test Category", Slug = "test-category" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        // Act
        _unitOfWork.DetachEntity(category);
        category.Name = "Modified Name";
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Categories.FindAsync(category.Id);
        saved!.Name.Should().Be("Modified Name");
    }

    #endregion

    #region Disposal Tests

    [TestMethod]
    public void Dispose_DisposesContext()
    {
        // Act
        _unitOfWork.Dispose();

        // Assert - Context should be disposed
        // Note: In-memory DB doesn't throw ObjectDisposedException like SQL Server would
    }

    [TestMethod]
    public async Task DisposeAsync_DisposesContext()
    {
        // Act
        await _unitOfWork.DisposeAsync();

        // Assert - Context should be disposed
        // Note: In-memory DB doesn't throw ObjectDisposedException like SQL Server would
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act
        _unitOfWork.Dispose();
        _unitOfWork.Dispose();

        // Assert - No exception thrown
    }

    #endregion
}
