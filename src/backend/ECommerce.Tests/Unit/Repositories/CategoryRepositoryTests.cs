using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories;

/// <summary>
/// Tests for the CategoryRepository class.
/// Tests category-specific repository operations including hierarchy and product counts.
/// </summary>
[TestClass]
public class CategoryRepositoryTests
{
    private AppDbContext _context = null!;
    private CategoryRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new CategoryRepository(_context);

        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Create parent categories
        var electronics = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic devices and accessories",
            IsActive = true
        };

        var clothing = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Clothing",
            Slug = "clothing",
            Description = "Apparel and fashion",
            IsActive = true
        };

        var inactiveCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Category",
            Slug = "inactive-category",
            Description = "This category is inactive",
            IsActive = false
        };

        _context.Categories.AddRange(electronics, clothing, inactiveCategory);

        // Create child categories
        var laptops = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Laptops",
            Slug = "laptops",
            Description = "Laptop computers",
            ParentId = electronics.Id,
            IsActive = true
        };

        var phones = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Phones",
            Slug = "phones",
            Description = "Mobile phones",
            ParentId = electronics.Id,
            IsActive = true
        };

        var mensClothing = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Men's Clothing",
            Slug = "mens-clothing",
            Description = "Clothing for men",
            ParentId = clothing.Id,
            IsActive = true
        };

        _context.Categories.AddRange(laptops, phones, mensClothing);

        // Create products
        var laptop1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Gaming Laptop",
            Slug = "gaming-laptop",
            Sku = "LAPTOP-001",
            Price = 1299.99m,
            StockQuantity = 10,
            CategoryId = laptops.Id,
            IsActive = true
        };

        var laptop2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Business Laptop",
            Slug = "business-laptop",
            Sku = "LAPTOP-002",
            Price = 999.99m,
            StockQuantity = 15,
            CategoryId = laptops.Id,
            IsActive = true
        };

        var phone1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Smartphone",
            Slug = "smartphone",
            Sku = "PHONE-001",
            Price = 699.99m,
            StockQuantity = 25,
            CategoryId = phones.Id,
            IsActive = true
        };

        var tshirt = new Product
        {
            Id = Guid.NewGuid(),
            Name = "T-Shirt",
            Slug = "t-shirt",
            Sku = "TSHIRT-001",
            Price = 29.99m,
            StockQuantity = 100,
            CategoryId = mensClothing.Id,
            IsActive = true
        };

        _context.Products.AddRange(laptop1, laptop2, phone1, tshirt);
        _context.SaveChanges();
    }

    #region GetBySlugAsync Tests

    [TestMethod]
    public async Task GetBySlugAsync_ExistingSlug_ReturnsCategory()
    {
        // Act
        var result = await _repository.GetBySlugAsync("electronics");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("electronics");
        result.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetBySlugAsync_IncludesParentAndChildren()
    {
        // Act
        var result = await _repository.GetBySlugAsync("electronics");

        // Assert
        result.Should().NotBeNull();
        result!.Children.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetBySlugAsync_IncludesProducts()
    {
        // Act
        var result = await _repository.GetBySlugAsync("laptops");

        // Assert
        result.Should().NotBeNull();
        result!.Products.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetBySlugAsync_NonExistingSlug_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBySlugAsync_InactiveCategory_ReturnsNull()
    {
        // Act
        var result = await _repository.GetBySlugAsync("inactive-category");

        // Assert - Should return null because only active categories are returned
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetBySlugAsync_WithTracking_TracksEntity()
    {
        // Act
        var result = await _repository.GetBySlugAsync("electronics", trackChanges: true);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Category>().Should().Contain(e => e.Entity.Id == result!.Id);
    }

    [TestMethod]
    public async Task GetBySlugAsync_WithoutTracking_DoesNotTrackEntity()
    {
        // Act
        var result = await _repository.GetBySlugAsync("electronics", trackChanges: false);

        // Assert
        result.Should().NotBeNull();
        _context.ChangeTracker.Entries<Category>().Should().NotContain(e => e.Entity.Id == result!.Id);
    }

    #endregion

    #region GetTopLevelCategoriesAsync Tests

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_ReturnsOnlyTopLevelCategories()
    {
        // Act
        var result = await _repository.GetTopLevelCategoriesAsync();

        // Assert
        result.Should().HaveCount(2); // Electronics and Clothing (not inactive)
        result.All(c => c.ParentId == null).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_ExcludesInactiveCategories()
    {
        // Act
        var result = await _repository.GetTopLevelCategoriesAsync();

        // Assert
        result.Should().NotContain(c => c.Slug == "inactive-category");
    }

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_IncludesChildren()
    {
        // Act
        var result = (await _repository.GetTopLevelCategoriesAsync()).ToList();

        // Assert
        var electronics = result.First(c => c.Slug == "electronics");
        electronics.Children.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_ReturnsInAlphabeticalOrder()
    {
        // Act
        var result = (await _repository.GetTopLevelCategoriesAsync()).ToList();

        // Assert
        result.Should().BeInAscendingOrder(c => c.Name);
    }

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_WithTracking_TracksEntities()
    {
        // Act
        var result = await _repository.GetTopLevelCategoriesAsync(trackChanges: true);

        // Assert
        foreach (var category in result)
        {
            _context.ChangeTracker.Entries<Category>().Should().Contain(e => e.Entity.Id == category.Id);
        }
    }

    [TestMethod]
    public async Task GetTopLevelCategoriesAsync_WithoutTracking_DoesNotTrackEntities()
    {
        // Act
        var result = await _repository.GetTopLevelCategoriesAsync(trackChanges: false);

        // Assert
        foreach (var category in result)
        {
            _context.ChangeTracker.Entries<Category>().Should().NotContain(e => e.Entity.Id == category.Id);
        }
    }

    #endregion

    #region GetCategoryWithChildrenAsync Tests

    [TestMethod]
    public async Task GetCategoryWithChildrenAsync_ReturnsChildren()
    {
        // Arrange
        var electronics = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.GetCategoryWithChildrenAsync(electronics.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetCategoryWithChildrenAsync_NonExistingCategory_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetCategoryWithChildrenAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetCategoryWithChildrenAsync_CategoryWithNoChildren_ReturnsEmpty()
    {
        // Arrange
        var laptops = await _context.Categories.FirstAsync(c => c.Slug == "laptops");

        // Act
        var result = await _repository.GetCategoryWithChildrenAsync(laptops.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetCategoryWithChildrenAsync_WithTracking_TracksEntities()
    {
        // Arrange
        var electronics = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.GetCategoryWithChildrenAsync(electronics.Id, trackChanges: true);

        // Assert
        foreach (var child in result)
        {
            _context.ChangeTracker.Entries<Category>().Should().Contain(e => e.Entity.Id == child.Id);
        }
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
        var result = await _repository.IsSlugUniqueAsync("electronics");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSlugUniqueAsync_ExistingSlugWithExclude_ReturnsTrue()
    {
        // Arrange
        var electronics = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.IsSlugUniqueAsync("electronics", excludeId: electronics.Id);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSlugUniqueAsync_ExistingSlugWithDifferentExclude_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsSlugUniqueAsync("electronics", excludeId: Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetProductCountAsync Tests

    [TestMethod]
    public async Task GetProductCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var laptops = await _context.Categories.FirstAsync(c => c.Slug == "laptops");

        // Act
        var result = await _repository.GetProductCountAsync(laptops.Id);

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task GetProductCountAsync_CategoryWithNoProducts_ReturnsZero()
    {
        // Arrange
        var electronics = await _context.Categories.FirstAsync(c => c.Slug == "electronics");

        // Act
        var result = await _repository.GetProductCountAsync(electronics.Id);

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task GetProductCountAsync_NonExistingCategory_ReturnsZero()
    {
        // Act
        var result = await _repository.GetProductCountAsync(Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Base Repository Tests

    [TestMethod]
    public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
    {
        // Arrange
        var category = await _context.Categories.FirstAsync();

        // Act
        var result = await _repository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingCategory_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(6); // 3 parent + 3 child categories
    }

    [TestMethod]
    public async Task AddAsync_AddsCategoryToDatabase()
    {
        // Arrange
        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "New Category",
            Slug = "new-category",
            IsActive = true
        };

        // Act
        await _repository.AddAsync(newCategory);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetBySlugAsync("new-category");
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Delete_RemovesCategoryFromDatabase()
    {
        // Arrange
        var category = await _context.Categories.FirstAsync(c => c.Slug == "inactive-category");

        // Act
        _repository.Delete(category);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(category.Id);
        result.Should().BeNull();
    }

    #endregion
}