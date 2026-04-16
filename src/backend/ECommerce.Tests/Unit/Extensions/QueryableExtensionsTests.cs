using System.Linq.Expressions;
using ECommerce.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Extensions;

[TestClass]
public class QueryableExtensionsTests
{
    private TestQueryableDbContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestQueryableDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestQueryableDbContext(options);
        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        _context.Products.AddRange(
            new TestProduct { Id = Guid.NewGuid(), Name = "Laptop", Price = 999.99m, StockQuantity = 10 },
            new TestProduct { Id = Guid.NewGuid(), Name = "Phone", Price = 599.99m, StockQuantity = 20 },
            new TestProduct { Id = Guid.NewGuid(), Name = "Tablet", Price = 399.99m, StockQuantity = 15 },
            new TestProduct { Id = Guid.NewGuid(), Name = "Smartwatch", Price = 299.99m, StockQuantity = 30 },
            new TestProduct { Id = Guid.NewGuid(), Name = "Headphones", Price = 149.99m, StockQuantity = 50 }
        );

        _context.SaveChanges();
    }

    [TestMethod]
    public async Task GetPagedDataAsync_ReturnsCorrectPage()
    {
        var (totalCount, items) = await _context.Products.AsQueryable().GetPagedDataAsync(1, 2);

        totalCount.ShouldBe(5);
        items.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetPagedDataAsync_EmptySource_ReturnsEmpty()
    {
        _context.Products.RemoveRange(_context.Products);
        await _context.SaveChangesAsync();

        var (totalCount, items) = await _context.Products.AsQueryable().GetPagedDataAsync(1, 2);

        totalCount.ShouldBe(0);
        items.ShouldBeEmpty();
    }

    [TestMethod]
    public void ApplySort_ByAscending_ReturnsSortedItems()
    {
        var result = _context.Products.AsQueryable().ApplySort("Price", ascending: true).ToList();

        result.Select(p => p.Price).ShouldBeInOrder();
    }

    [TestMethod]
    public void ApplySort_ByDescending_ReturnsSortedItems()
    {
        var result = _context.Products.AsQueryable().ApplySort("Price", ascending: false).ToList();

        result.Select(p => p.Price).ShouldBeInOrder(SortDirection.Descending);
    }

    [TestMethod]
    public void ApplySort_InvalidProperty_ReturnsOriginalOrder()
    {
        var result = _context.Products.AsQueryable().ApplySort("InvalidProperty", ascending: true).ToList();

        result.Count.ShouldBe(5);
    }

    [TestMethod]
    public void Where_WithPredicate_FiltersItems()
    {
        Expression<Func<TestProduct, bool>> predicate = p => p.Price > 500;
        var result = Queryable.Where(_context.Products.AsQueryable(), predicate).ToList();

        result.Count.ShouldBe(2);
        result.All(p => p.Price > 500).ShouldBeTrue();
    }

    [TestMethod]
    public void Where_WithNullPredicate_ReturnsAllItems()
    {
        var result = QueryableExtensions.Where(_context.Products.AsQueryable(), null).ToList();

        result.Count.ShouldBe(5);
    }

    [TestMethod]
    public void InRange_ReturnsItemsWithinRange()
    {
        var result = _context.Products.AsQueryable().InRange(p => p.Price, 200m, 600m).ToList();

        result.Count.ShouldBe(3);
        result.All(p => p.Price >= 200m && p.Price <= 600m).ShouldBeTrue();
    }

    [TestMethod]
    public void GreaterThan_ReturnsItemsGreaterThanValue()
    {
        var result = _context.Products.AsQueryable().GreaterThan(p => p.Price, 500m).ToList();

        result.Count.ShouldBe(2);
        result.All(p => p.Price > 500m).ShouldBeTrue();
    }

    [TestMethod]
    public void LessThan_ReturnsItemsLessThanValue()
    {
        var result = _context.Products.AsQueryable().LessThan(p => p.Price, 300m).ToList();

        result.Count.ShouldBe(2);
        result.All(p => p.Price < 300m).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Combined_FilterSortAndPaginate_ReturnsCorrectResults()
    {
        var (totalCount, items) = await _context.Products.AsQueryable()
            .GreaterThan(p => p.Price, 200m)
            .ApplySort("Name", ascending: true)
            .GetPagedDataAsync(1, 2);

        totalCount.ShouldBe(4);
        items.Count.ShouldBe(2);
        items.Select(p => p.Name).ShouldBeInOrder();
    }

    private sealed class TestQueryableDbContext(DbContextOptions<TestQueryableDbContext> options) : DbContext(options)
    {
        public DbSet<TestProduct> Products => Set<TestProduct>();
    }

    private sealed class TestProduct
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
