using AutoMapper;
using ECommerce.Application.DTOs;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _mockCategoryRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private CategoryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockMapper = MockHelpers.CreateMockMapper();

        _service = new CategoryService(_mockCategoryRepository.Object, _mockMapper.Object);
    }

    [TestMethod]
    public async Task GetAllCategoriesAsync_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            TestDataFactory.CreateCategory("Cat1"),
            TestDataFactory.CreateCategory("Cat2")
        };

        _mockCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
        _mockMapper.Setup(m => m.Map<IEnumerable<CategoryDto>>(It.IsAny<IEnumerable<Category>>()))
            .Returns((IEnumerable<Category> src) => src.Select(c => new CategoryDto { Id = c.Id, Name = c.Name }).ToList());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetCategoryByIdAsync_ExistingId_ReturnsCategory()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory("MyCat");
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(5);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>()))
            .Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name });

        // Act
        var result = await _service.GetCategoryByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(category.Id);
        result.ProductCount.Should().Be(5);
    }

    [TestMethod]
    public async Task GetCategoryByIdAsync_NonExistentId_ThrowsCategoryNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _service.GetCategoryByIdAsync(id);

        // Assert
        await act.Should().ThrowAsync<CategoryNotFoundException>();
    }

    [TestMethod]
    public async Task GetCategoryBySlugAsync_ExistingSlug_ReturnsCategory()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory("SlugCat", slug: "slug-cat");
        _mockCategoryRepository.Setup(r => r.GetBySlugAsync("slug-cat")).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(2);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>()))
            .Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name, Slug = c.Slug });

        // Act
        var result = await _service.GetCategoryBySlugAsync("slug-cat");

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().Be("slug-cat");
        result.ProductCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetCategoryBySlugAsync_NonExistentSlug_ThrowsCategoryNotFoundException()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.GetBySlugAsync("missing")).ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _service.GetCategoryBySlugAsync("missing");

        // Assert
        await act.Should().ThrowAsync<CategoryNotFoundException>();
    }

    [TestMethod]
    public async Task CreateCategoryAsync_ValidData_ReturnsCreatedCategory()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "NewCat", Slug = "new-cat" };
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map<Category>(It.IsAny<CreateCategoryDto>()))
            .Returns((CreateCategoryDto d) => new Category { Id = Guid.NewGuid(), Name = d.Name, Slug = d.Slug });
        _mockCategoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => c);
        _mockCategoryRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>()))
            .Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name, Slug = c.Slug });

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().Be(dto.Slug);
        _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateCategoryAsync_DuplicateSlug_ThrowsDuplicateCategorySlugException()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "Dup", Slug = "dup" };
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug)).ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.CreateCategoryAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateCategorySlugException>();
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_ValidData_ReturnsUpdatedCategory()
    {
        // Arrange
        var existing = TestDataFactory.CreateCategory("OldCat", slug: "old-cat");
        var dto = new UpdateCategoryDto { Name = "Updated", Slug = "updated-cat" };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, existing.Id)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map(dto, existing)).Callback(() =>
        {
            existing.Name = dto.Name!;
            existing.Slug = dto.Slug!;
        });
        _mockCategoryRepository.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);
        _mockCategoryRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>())).Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name, Slug = c.Slug });

        // Act
        var result = await _service.UpdateCategoryAsync(existing.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated");
        _mockCategoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_NonExistentId_ThrowsCategoryNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateCategoryDto { Name = "Nope" };
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateCategoryAsync(id, dto);

        // Assert
        await act.Should().ThrowAsync<CategoryNotFoundException>();
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_ExistingId_DeletesCategory()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(0);
        _mockCategoryRepository.Setup(r => r.DeleteAsync(category)).Returns(Task.CompletedTask);
        _mockCategoryRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.DeleteCategoryAsync(category.Id);

        // Assert
        _mockCategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Once);
        _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_NonExistentId_ThrowsCategoryNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _service.DeleteCategoryAsync(id);

        // Assert
        await act.Should().ThrowAsync<CategoryNotFoundException>();
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_CategoryWithProducts_ThrowsCategoryHasProductsException()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(3);

        // Act
        Func<Task> act = async () => await _service.DeleteCategoryAsync(category.Id);

        // Assert
        await act.Should().ThrowAsync<CategoryHasProductsException>();
    }
}
