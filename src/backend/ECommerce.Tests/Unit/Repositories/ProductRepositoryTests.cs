using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the ProductRepository class.
/// Tests product-specific repository operations including filtering, searching, and stock management.
/// </summary>
[TestClass]
public class ProductRepositoryTests
{
    private AppDbContext _context = null!;
    private ProductRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ProductRepository(_context);

        // Seed test data
        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var category1 = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics" };
        var category2 = new Category { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing" };

        _context.Categories.AddRange(category1, category2);

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Slug = "laptop",
                Description = "High performance laptop",
                Sku = "LAPTOP-001",
                Price = 999.99m,
                StockQuantity = 10,
                CategoryId = category1.Id,
                IsActive = true,
                IsFeatured = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Slug = "phone",
                Description = "Smart phone",
                Sku = "PHONE-001",
                Price = 599.99m,
                StockQuantity = 20,
                CategoryId = category1.Id,
                IsActive = true,
                IsFeatured = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "T-Shirt",
                Slug = "t-shirt",
                Description = "Cotton t-shirt",
                Sku = "TSHIRT-001",
                Price = 29.99m,
                StockQuantity = 50,
                CategoryId = category2.Id,
                IsActive = true,
                IsFeatured = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Product",
                Slug = "inactive-product",
                Description = "This product is inactive",
                Sku = "INACTIVE-001",
                Price = 99.99m,
                StockQuantity = 5,
                CategoryId = category1.Id,
                IsActive = false,
                IsFeatured = false
            }
        };

        _context.Products.AddRange(products);

        // Add reviews for rating tests
        var laptopId = products.First(p => p.Slug == "laptop").Id;
        _context.Reviews.AddRange(
            new Review
            {
                Id = Guid.NewGuid(),
                ProductId = laptopId,
                Rating = 5,
                Comment = "Great laptop!",
                IsApproved = true,
                UserId = Guid.NewGuid()
            },
            new Review
            {
                Id = Guid.NewGuid(),
                ProductId = laptopId,
                Rating = 4,
                Comment = "Good laptop",
                IsApproved = true,
                UserId = Guid.NewGuid()
            }
        );

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    #region GetBySlugAsync Tests

    [TestMethod]
    public async Task GetBySlugAsync_ExistingSlug_ReturnsProduct()
    {
        // Act
        var result = await _repository.GetBySlugAsync("laptop");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("laptop");
        result.Name.Should().Be("Laptop");
    }

    [TestMethod]
    public async Task GetBySlugAsync_NonExistingSlug_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBySlugAsync_InactiveProduct_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("inactive-product");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBySlugAsync_IncludesRelatedData()
    {
        // Act
        var result = await _repository.GetBySlugAsync("laptop");

        // Assert
        result.Should().NotBeNull();
        result!.Category.Should().NotBeNull();
        result.Images.Should().NotBeNull();
        result.Reviews.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetBySlugAsync_WithTracking_TracksChanges()
    {
        // Arrange
        var product = await _repository.GetBySlugAsync("laptop", trackChanges: true);

        // Act
        product!.Name = "Modified Laptop";
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Products.FirstOrDefaultAsync(p => p.Slug == "laptop");
        updated!.Name.Should().Be("Modified Laptop");
    }

    #endregion

    #region GetByCategoryAsync Tests

    [TestMethod]
    public async Task GetByCategoryAsync_ReturnsProductsInCategory()
    {
        // Arrange
        var electronicsCategory = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.GetByCategoryAsync(electronicsCategory.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.All(p => p.CategoryId == electronicsCategory.Id).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByCategoryAsync_ExcludesInactiveProducts()
    {
        // Arrange
        var electronicsCategory = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.GetByCategoryAsync(electronicsCategory.Id);

        // Assert
        result.All(p => p.IsActive).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByCategoryAsync_EmptyCategory_ReturnsEmpty()
    {
        // Arrange
        var newCategoryId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByCategoryAsync(newCategoryId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFeaturedAsync Tests

    [TestMethod]
    public async Task GetFeaturedAsync_ReturnsFeaturedProducts()
    {
        // Act
        var result = await _repository.GetFeaturedAsync(10);

        // Assert
        result.Should().NotBeEmpty();
        result.All(p => p.IsFeatured).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetFeaturedAsync_RespectsCount()
    {
        // Act
        var result = await _repository.GetFeaturedAsync(1);

        // Assert
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task GetFeaturedAsync_ExcludesInactiveProducts()
    {
        // Act
        var result = await _repository.GetFeaturedAsync(10);

        // Assert
        result.All(p => p.IsActive).Should().BeTrue();
    }

    #endregion

    #region GetActiveProductsAsync Tests

    [TestMethod]
    public async Task GetActiveProductsAsync_ReturnsActiveProducts()
    {
        // Act
        var result = await _repository.GetActiveProductsAsync(0, 10);

        // Assert
        result.Should().NotBeEmpty();
        result.All(p => p.IsActive).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetActiveProductsAsync_RespectsPagination()
    {
        // Act
        var page1 = await _repository.GetActiveProductsAsync(0, 2);
        var page2 = await _repository.GetActiveProductsAsync(2, 2);

        // Assert
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1); // Only 3 active products total
    }

    #endregion

    #region GetActiveProductsCountAsync Tests

    [TestMethod]
    public async Task GetActiveProductsCountAsync_ReturnsCorrectCount()
    {
        // Act
        var result = await _repository.GetActiveProductsCountAsync();

        // Assert
        result.Should().Be(3); // 3 active products seeded
    }

    #endregion

    #region UpdateStockAsync Tests

    [TestMethod]
    public async Task UpdateStockAsync_UpdatesStockQuantity()
    {
        // Arrange
        var product = await _context.Products.FirstAsync(p => p.Slug == "laptop");
        var originalStock = product.StockQuantity;

        // Act
        await _repository.UpdateStockAsync(product.Id, 100);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Products.FindAsync(product.Id);
        updated!.StockQuantity.Should().Be(100);
        updated.StockQuantity.Should().NotBe(originalStock);
    }

    [TestMethod]
    public async Task UpdateStockAsync_UpdatesUpdatedAt()
    {
        // Arrange
        var product = await _context.Products.FirstAsync(p => p.Slug == "laptop");
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _repository.UpdateStockAsync(product.Id, 50);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Products.FindAsync(product.Id);
        updated!.UpdatedAt.Should().NotBe(default(DateTime));
        updated.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [TestMethod]
    public async Task UpdateStockAsync_NonExistingProduct_DoesNothing()
    {
        // Act
        await _repository.UpdateStockAsync(Guid.NewGuid(), 100);
        await _context.SaveChangesAsync();

        // Assert - No exception thrown
    }

    #endregion

    #region IsSlugUniqueAsync Tests

    [TestMethod]
    public async Task IsSlugUniqueAsync_UniqueSlug_ReturnsTrue()
    {
        // Act
        var result = await _repository.IsSlugUniqueAsync("new-unique-slug");

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSlugUniqueAsync_ExistingSlug_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsSlugUniqueAsync("laptop");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSlugUniqueAsync_WithExcludeId_ReturnsTrueForSameProduct()
    {
        // Arrange
        var product = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var result = await _repository.IsSlugUniqueAsync("laptop", product.Id);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSlugUniqueAsync_WithExcludeId_ReturnsFalseForOtherProduct()
    {
        // Arrange
        var otherProductId = Guid.NewGuid();

        // Act
        var result = await _repository.IsSlugUniqueAsync("laptop", otherProductId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetProductsWithFiltersAsync Tests

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_NoFilters_ReturnsAllActive()
    {
        // Act
        var (items, totalCount) = await _repository.GetProductsWithFiltersAsync(0, 10);

        // Assert
        items.Should().HaveCount(3);
        totalCount.Should().Be(3);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_ByCategory_ReturnsFiltered()
    {
        // Arrange
        var category = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var (items, totalCount) = await _repository.GetProductsWithFiltersAsync(0, 10, categoryId: category.Id);

        // Assert
        items.Should().HaveCount(2);
        items.All(p => p.CategoryId == category.Id).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_BySearchQuery_ReturnsMatching()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, searchQuery: "laptop");

        // Assert
        items.Should().HaveCount(1);
        items.First().Name.Should().Contain("Laptop");
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_ByPriceRange_ReturnsFiltered()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, minPrice: 100, maxPrice: 700);

        // Assert
        items.Should().HaveCount(1);
        items.First().Price.Should().Be(599.99m);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_ByFeatured_ReturnsFeatured()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, isFeatured: true);

        // Assert
        items.Should().HaveCount(2);
        items.All(p => p.IsFeatured).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_SortByName_SortsCorrectly()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, sortBy: "name");

        // Assert
        items.Should().BeInAscendingOrder(p => p.Name);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_SortByPriceAsc_SortsCorrectly()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, sortBy: "price-asc");

        // Assert
        items.Should().BeInAscendingOrder(p => p.Price);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_SortByPriceDesc_SortsCorrectly()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, sortBy: "price-desc");

        // Assert
        items.Should().BeInDescendingOrder(p => p.Price);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_SortByNewest_SortsCorrectly()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, sortBy: "newest");

        // Assert
        items.Should().BeInDescendingOrder(p => p.CreatedAt);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_Pagination_WorksCorrectly()
    {
        // Act
        var (page1, total) = await _repository.GetProductsWithFiltersAsync(0, 2);
        var (page2, _) = await _repository.GetProductsWithFiltersAsync(2, 2);

        // Assert
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1);
        total.Should().Be(3);
    }

    [TestMethod]
    public async Task GetProductsWithFiltersAsync_ByMinRating_ReturnsFiltered()
    {
        // Act
        var (items, _) = await _repository.GetProductsWithFiltersAsync(0, 10, minRating: 4.0m);

        // Assert
        items.Should().HaveCount(1);
        items.First().Slug.Should().Be("laptop");
    }

    #endregion

    #region TryReduceStockAsync Tests

    // Note: TryReduceStockAsync uses raw SQL which doesn't work with InMemory provider.
    // The successful case would need an integration test with a real database.
    // Only validation tests are included here.

    [TestMethod]
    public async Task TryReduceStockAsync_ZeroQuantity_ThrowsException()
    {
        // Arrange
        var product = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var act = () => _repository.TryReduceStockAsync(product.Id, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task TryReduceStockAsync_NegativeQuantity_ThrowsException()
    {
        // Arrange
        var product = await _context.Products.FirstAsync(p => p.Slug == "laptop");

        // Act
        var act = () => _repository.TryReduceStockAsync(product.Id, -5);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
