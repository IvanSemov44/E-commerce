using System.Linq.Expressions;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Extensions;

/// <summary>
/// Tests for the QueryableExtensions class.
/// Tests pagination, sorting, filtering, and searching extension methods.
/// </summary>
[TestClass]
public class QueryableExtensionsTests
{
    private AppDbContext _context = null!;
    private List<Product> _testProducts = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
        _context.Categories.Add(category);

        _testProducts = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Slug = "laptop",
                Sku = "LAPTOP-001",
                Description = "High performance laptop",
                Price = 999.99m,
                StockQuantity = 10,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Slug = "phone",
                Sku = "PHONE-001",
                Description = "Smart phone",
                Price = 599.99m,
                StockQuantity = 20,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Tablet",
                Slug = "tablet",
                Sku = "TABLET-001",
                Description = "Tablet device",
                Price = 399.99m,
                StockQuantity = 15,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Smartwatch",
                Slug = "smartwatch",
                Sku = "WATCH-001",
                Description = "Smart watch",
                Price = 299.99m,
                StockQuantity = 30,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Headphones",
                Slug = "headphones",
                Sku = "HEAD-001",
                Description = "Wireless headphones",
                Price = 149.99m,
                StockQuantity = 50,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            }
        };

        _context.Products.AddRange(_testProducts);
        _context.SaveChanges();
    }

    #region GetPagedDataAsync Tests

    [TestMethod]
    public async Task GetPagedDataAsync_ReturnsCorrectPage()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query.GetPagedDataAsync(1, 2);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetPagedDataAsync_SecondPage_ReturnsCorrectItems()
    {
        // Arrange
        var query = _context.Products.OrderBy(p => p.Name).AsQueryable();

        // Act
        var (totalCount, items) = await query.GetPagedDataAsync(2, 2);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetPagedDataAsync_LastPage_ReturnsRemainingItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query.GetPagedDataAsync(3, 2);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetPagedDataAsync_PageBeyondData_ReturnsEmpty()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query.GetPagedDataAsync(10, 2);

        // Assert
        totalCount.Should().Be(5);
        items.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetPagedDataAsync_EmptySource_ReturnsEmpty()
    {
        // Arrange
        _context.Products.RemoveRange(_context.Products);
        await _context.SaveChangesAsync();
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query.GetPagedDataAsync(1, 2);

        // Assert
        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    #endregion

    #region ApplySort Tests

    [TestMethod]
    public void ApplySort_ByAscending_ReturnsSortedItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("Price", ascending: true).ToList();

        // Assert
        result.Should().BeInAscendingOrder(p => p.Price);
    }

    [TestMethod]
    public void ApplySort_ByDescending_ReturnsSortedItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("Price", ascending: false).ToList();

        // Assert
        result.Should().BeInDescendingOrder(p => p.Price);
    }

    [TestMethod]
    public void ApplySort_ByName_ReturnsSortedItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("Name", ascending: true).ToList();

        // Assert
        result.Should().BeInAscendingOrder(p => p.Name);
    }

    [TestMethod]
    public void ApplySort_ByStockQuantity_ReturnsSortedItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("StockQuantity", ascending: true).ToList();

        // Assert
        result.Should().BeInAscendingOrder(p => p.StockQuantity);
    }

    [TestMethod]
    public void ApplySort_EmptySortField_ReturnsOriginalOrder()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("", ascending: true).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [TestMethod]
    public void ApplySort_NullSortField_ReturnsOriginalOrder()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort(null!, ascending: true).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [TestMethod]
    public void ApplySort_InvalidProperty_ReturnsOriginalOrder()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("InvalidProperty", ascending: true).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [TestMethod]
    public void ApplySort_CaseInsensitiveProperty_ReturnsSortedItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.ApplySort("PRICE", ascending: true).ToList();

        // Assert
        result.Should().BeInAscendingOrder(p => p.Price);
    }

    #endregion

    #region Where (with nullable predicate) Tests

    [TestMethod]
    public void Where_WithPredicate_FiltersItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();
        Expression<Func<Product, bool>> predicate = p => p.Price > 500;

        // Act
        var result = System.Linq.Queryable.Where(query, predicate).ToList();

        // Assert
        result.Should().HaveCount(2); // Laptop (999.99) and Phone (599.99)
        result.All(p => p.Price > 500).Should().BeTrue();
    }

    [TestMethod]
    public void Where_WithNullPredicate_ReturnsAllItems()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = QueryableExtensions.Where(query, null).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region InRange Tests

    [TestMethod]
    public void InRange_ReturnsItemsWithinRange()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.InRange(p => p.Price, 200m, 600m).ToList();

        // Assert
        result.Should().HaveCount(3); // Phone (599.99), Tablet (399.99), Smartwatch (299.99)
        result.All(p => p.Price >= 200m && p.Price <= 600m).Should().BeTrue();
    }

    [TestMethod]
    public void InRange_InclusiveMinAndMax()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.InRange(p => p.StockQuantity, 10, 20).ToList();

        // Assert
        result.Should().HaveCount(3); // Laptop (10), Phone (20), Tablet (15)
    }

    [TestMethod]
    public void InRange_NoItemsInRange_ReturnsEmpty()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.InRange(p => p.Price, 5000m, 10000m).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void InRange_AllItemsInRange_ReturnsAll()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.InRange(p => p.Price, 0m, 2000m).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region GreaterThan Tests

    [TestMethod]
    public void GreaterThan_ReturnsItemsGreaterThanValue()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.GreaterThan(p => p.Price, 500m).ToList();

        // Assert
        result.Should().HaveCount(2); // Laptop (999.99) and Phone (599.99)
        result.All(p => p.Price > 500m).Should().BeTrue();
    }

    [TestMethod]
    public void GreaterThan_ExcludesEqualValue()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.GreaterThan(p => p.StockQuantity, 20).ToList();

        // Assert
        result.Should().HaveCount(2); // Smartwatch (30) and Headphones (50)
        result.Should().NotContain(p => p.StockQuantity == 20);
    }

    [TestMethod]
    public void GreaterThan_NoItemsGreater_ReturnsEmpty()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.GreaterThan(p => p.Price, 5000m).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region LessThan Tests

    [TestMethod]
    public void LessThan_ReturnsItemsLessThanValue()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.LessThan(p => p.Price, 300m).ToList();

        // Assert
        result.Should().HaveCount(2); // Smartwatch (299.99) and Headphones (149.99)
        result.All(p => p.Price < 300m).Should().BeTrue();
    }

    [TestMethod]
    public void LessThan_ExcludesEqualValue()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.LessThan(p => p.StockQuantity, 20).ToList();

        // Assert
        result.Should().HaveCount(2); // Laptop (10) and Tablet (15)
        result.Should().NotContain(p => p.StockQuantity == 20);
    }

    [TestMethod]
    public void LessThan_NoItemsLess_ReturnsEmpty()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var result = query.LessThan(p => p.Price, 100m).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Combined Operations Tests

    [TestMethod]
    public async Task Combined_SortAndPaginate_ReturnsCorrectResults()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query
            .ApplySort("Price", ascending: false)
            .GetPagedDataAsync(1, 3);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(3);
        items.Should().BeInDescendingOrder(p => p.Price);
    }

    [TestMethod]
    public async Task Combined_FilterSortAndPaginate_ReturnsCorrectResults()
    {
        // Arrange
        var query = _context.Products.AsQueryable();

        // Act
        var (totalCount, items) = await query
            .GreaterThan(p => p.Price, 200m)
            .ApplySort("Name", ascending: true)
            .GetPagedDataAsync(1, 2);

        // Assert
        totalCount.Should().Be(4); // Phone, Smartwatch, Tablet, Laptop
        items.Should().HaveCount(2);
        items.Should().BeInAscendingOrder(p => p.Name);
    }

    #endregion
}
