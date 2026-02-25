using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the OrderRepository class.
/// Tests order-specific repository operations including retrieval, pagination, and analytics.
/// </summary>
[TestClass]
public class OrderRepositoryTests
{
    private AppDbContext _context = null!;
    private OrderRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new OrderRepository(_context);

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

        var product = new Product
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
        _context.Products.Add(product);

        var address1 = new Address
        {
            Id = Guid.NewGuid(),
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            PostalCode = "10001",
            Country = "USA"
        };
        var address2 = new Address
        {
            Id = Guid.NewGuid(),
            Street = "456 Oak Ave",
            City = "Los Angeles",
            State = "CA",
            PostalCode = "90001",
            Country = "USA"
        };
        _context.Addresses.AddRange(address1, address2);

        var promoCode = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = "SAVE10",
            DiscountPercentage = 10,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidUntil = DateTime.UtcNow.AddDays(10)
        };
        _context.PromoCodes.Add(promoCode);

        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-001",
                UserId = user1.Id,
                User = user1,
                ShippingAddressId = address1.Id,
                ShippingAddress = address1,
                BillingAddressId = address1.Id,
                BillingAddress = address1,
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 999.99m,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-002",
                UserId = user1.Id,
                User = user1,
                ShippingAddressId = address2.Id,
                ShippingAddress = address2,
                BillingAddressId = address2.Id,
                BillingAddress = address2,
                Status = OrderStatus.Processing,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 500.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-003",
                UserId = user2.Id,
                User = user2,
                ShippingAddressId = address1.Id,
                ShippingAddress = address1,
                BillingAddressId = address1.Id,
                BillingAddress = address1,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount = 250.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-004",
                UserId = user2.Id,
                User = user2,
                ShippingAddressId = address2.Id,
                ShippingAddress = address2,
                BillingAddressId = address2.Id,
                BillingAddress = address2,
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                PromoCodeId = promoCode.Id,
                PromoCode = promoCode,
                TotalAmount = 750.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        _context.Orders.AddRange(orders);

        // Add order items
        var orderItems = new List<OrderItem>
        {
            new() { Id = Guid.NewGuid(), OrderId = orders[0].Id, ProductId = product.Id, Product = product, Quantity = 1, UnitPrice = 999.99m },
            new() { Id = Guid.NewGuid(), OrderId = orders[1].Id, ProductId = product.Id, Product = product, Quantity = 1, UnitPrice = 500.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[2].Id, ProductId = product.Id, Product = product, Quantity = 1, UnitPrice = 250.00m },
            new() { Id = Guid.NewGuid(), OrderId = orders[3].Id, ProductId = product.Id, Product = product, Quantity = 1, UnitPrice = 750.00m }
        };

        _context.OrderItems.AddRange(orderItems);
        _context.SaveChanges();
    }

    #region GetByOrderNumberAsync Tests

    [TestMethod]
    public async Task GetByOrderNumberAsync_ExistingOrder_ReturnsOrderWithAllRelations()
    {
        // Act
        var result = await _repository.GetByOrderNumberAsync("ORD-001");

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("ORD-001");
        result.User.Should().NotBeNull();
        result.ShippingAddress.Should().NotBeNull();
        result.BillingAddress.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetByOrderNumberAsync_NonExistingOrder_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByOrderNumberAsync("NON-EXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByOrderNumberAsync_WithPromoCode_ReturnsOrderWithPromoCode()
    {
        // Act
        var result = await _repository.GetByOrderNumberAsync("ORD-004");

        // Assert
        result.Should().NotBeNull();
        result!.PromoCode.Should().NotBeNull();
        result.PromoCode!.Code.Should().Be("SAVE10");
    }

    [TestMethod]
    public async Task GetByOrderNumberAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetByOrderNumberAsync("ORD-001", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Order>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetByOrderNumberAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetByOrderNumberAsync("ORD-001", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Order>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetUserOrdersAsync Tests

    [TestMethod]
    public async Task GetUserOrdersAsync_ReturnsOrdersForSpecificUser()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetUserOrdersAsync(user1.Id, 0, 10);

        // Assert
        result.Should().HaveCount(2);
        result.All(o => o.UserId == user1.Id).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetUserOrdersAsync_ReturnsOrdersInDescendingOrder()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetUserOrdersAsync(user1.Id, 0, 10);

        // Assert
        result.Should().BeInDescendingOrder(o => o.CreatedAt);
    }

    [TestMethod]
    public async Task GetUserOrdersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetUserOrdersAsync(user1.Id, 0, 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetUserOrdersAsync_WithSkip_SkipsCorrectNumberOfOrders()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var allOrders = (await _repository.GetUserOrdersAsync(user1.Id, 0, 10)).ToList();
        var skippedOrders = (await _repository.GetUserOrdersAsync(user1.Id, 1, 10)).ToList();

        // Assert
        skippedOrders.First().Id.Should().Be(allOrders[1].Id);
    }

    [TestMethod]
    public async Task GetUserOrdersAsync_IncludesItemsAndProducts()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetUserOrdersAsync(user1.Id, 0, 10);

        // Assert
        result.Should().AllSatisfy(o =>
        {
            o.Items.Should().NotBeEmpty();
            o.Items.First().Product.Should().NotBeNull();
        });
    }

    [TestMethod]
    public async Task GetUserOrdersAsync_UserWithNoOrders_ReturnsEmptyList()
    {
        // Arrange
        var newUser = new User { Id = Guid.NewGuid(), Email = "new@test.com", FirstName = "New", LastName = "User" };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserOrdersAsync(newUser.Id, 0, 10);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserOrdersCountAsync Tests

    [TestMethod]
    public async Task GetUserOrdersCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var user1 = await _context.Users.FirstAsync(u => u.Email == "user1@test.com");

        // Act
        var result = await _repository.GetUserOrdersCountAsync(user1.Id);

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task GetUserOrdersCountAsync_UserWithNoOrders_ReturnsZero()
    {
        // Arrange
        var newUser = new User { Id = Guid.NewGuid(), Email = "new@test.com", FirstName = "New", LastName = "User" };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserOrdersCountAsync(newUser.Id);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetWithItemsAsync Tests

    [TestMethod]
    public async Task GetWithItemsAsync_ExistingOrder_ReturnsOrderWithAllRelations()
    {
        // Arrange
        var order = await _context.Orders.FirstAsync(o => o.OrderNumber == "ORD-001");

        // Act
        var result = await _repository.GetWithItemsAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.ShippingAddress.Should().NotBeNull();
        result.BillingAddress.Should().NotBeNull();
        result.User.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetWithItemsAsync_NonExistingOrder_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithItemsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWithItemsAsync_IncludesProductInItems()
    {
        // Arrange
        var order = await _context.Orders.FirstAsync(o => o.OrderNumber == "ORD-001");

        // Act
        var result = await _repository.GetWithItemsAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.First().Product.Should().NotBeNull();
    }

    #endregion

    #region GetAllOrdersPaginatedAsync Tests

    [TestMethod]
    public async Task GetAllOrdersPaginatedAsync_ReturnsAllOrders()
    {
        // Act
        var result = await _repository.GetAllOrdersPaginatedAsync(0, 10);

        // Assert
        result.Should().HaveCount(4);
    }

    [TestMethod]
    public async Task GetAllOrdersPaginatedAsync_ReturnsInDescendingOrder()
    {
        // Act
        var result = await _repository.GetAllOrdersPaginatedAsync(0, 10);

        // Assert
        result.Should().BeInDescendingOrder(o => o.CreatedAt);
    }

    [TestMethod]
    public async Task GetAllOrdersPaginatedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var result = await _repository.GetAllOrdersPaginatedAsync(0, 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetAllOrdersPaginatedAsync_WithSkip_SkipsCorrectOrders()
    {
        // Act
        var allOrders = (await _repository.GetAllOrdersPaginatedAsync(0, 10)).ToList();
        var skippedOrders = (await _repository.GetAllOrdersPaginatedAsync(2, 10)).ToList();

        // Assert
        skippedOrders.First().Id.Should().Be(allOrders[2].Id);
    }

    #endregion

    #region GetTotalOrdersCountAsync Tests

    [TestMethod]
    public async Task GetTotalOrdersCountAsync_ReturnsCorrectCount()
    {
        // Act
        var result = await _repository.GetTotalOrdersCountAsync();

        // Assert
        result.Should().Be(4);
    }

    #endregion

    #region GetTotalRevenueAsync Tests

    [TestMethod]
    public async Task GetTotalRevenueAsync_ReturnsSumOfPaidOrders()
    {
        // Act
        var result = await _repository.GetTotalRevenueAsync();

        // Assert - ORD-001 (999.99) + ORD-002 (500) + ORD-004 (750) = 2249.99
        result.Should().BeApproximately(2249.99m, 0.01m);
    }

    [TestMethod]
    public async Task GetTotalRevenueAsync_ExcludesPendingPayments()
    {
        // Arrange - Get pending order amount
        var pendingOrder = await _context.Orders.FirstAsync(o => o.PaymentStatus == PaymentStatus.Pending);

        // Act
        var result = await _repository.GetTotalRevenueAsync();

        // Assert - Should not include pending order (250.00)
        result.Should().BeLessThan(2500m);
    }

    #endregion

    #region GetOrdersTrendAsync Tests

    [TestMethod]
    public async Task GetOrdersTrendAsync_ReturnsTrendData()
    {
        // Act
        var result = await _repository.GetOrdersTrendAsync(10);

        // Assert
        result.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetOrdersTrendAsync_OnlyIncludesOrdersWithinDateRange()
    {
        // Act
        var result = await _repository.GetOrdersTrendAsync(1);

        // Assert - Should only include orders from last 1 day
        result.Should().NotBeNull();
    }

    #endregion

    #region GetRevenueTrendAsync Tests

    [TestMethod]
    public async Task GetRevenueTrendAsync_ReturnsRevenueData()
    {
        // Act
        var result = await _repository.GetRevenueTrendAsync(10);

        // Assert
        result.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetRevenueTrendAsync_OnlyIncludesPaidOrders()
    {
        // Act
        var result = await _repository.GetRevenueTrendAsync(10);

        // Assert - All revenue should be from paid orders
        result.Values.Should().AllBeGreaterThan(0);
    }

    #endregion
}