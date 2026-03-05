using AutoMapper;
    using ECommerce.Application.DTOs.Common;
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
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _mockCategoryRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<CategoryService>> _mockLogger = null!;
    private CategoryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockLogger = new Mock<ILogger<CategoryService>>();

        _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);

        _service = new CategoryService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
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

        _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _mockMapper.Setup(m => m.Map<IEnumerable<CategoryDto>>(It.IsAny<IEnumerable<Category>>()))
            .Returns((IEnumerable<Category> src) => src.Select(c => new CategoryDto { Id = c.Id, Name = c.Name }).ToList());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetCategoryByIdAsync_ExistingId_ReturnsCategory()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory("MyCat");
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<bool>())).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(5);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>()))
            .Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name });

        // Act
        var result = await _service.GetCategoryByIdAsync(category.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CategoryDetailDto>.Success success)
        {
            success.Data.Id.Should().Be(category.Id);
            success.Data.ProductCount.Should().Be(5);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Success");
        }
    }

    [TestMethod]
    public async Task GetCategoryByIdAsync_NonExistentId_ReturnsCategoryNotFoundFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Category?)null);

        // Act
        var result = await _service.GetCategoryByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CategoryDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CategoryNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Failure");
        }
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
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CategoryDetailDto>.Success success)
        {
            success.Data.Slug.Should().Be("slug-cat");
            success.Data.ProductCount.Should().Be(2);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Success");
        }
    }

    [TestMethod]
    public async Task GetCategoryBySlugAsync_NonExistentSlug_ReturnsCategoryNotFoundFailure()
    {
        // Arrange
        _mockCategoryRepository.Setup(r => r.GetBySlugAsync("missing")).ReturnsAsync((Category?)null);

        // Act
        var result = await _service.GetCategoryBySlugAsync("missing");

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CategoryDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CategoryNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task CreateCategoryAsync_ValidData_ReturnsCreatedCategory()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "NewCat", Slug = "new-cat" };
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map<Category>(It.IsAny<CreateCategoryDto>()))
            .Returns((CreateCategoryDto d) => new Category { Id = Guid.NewGuid(), Name = d.Name, Slug = d.Slug });
        _mockCategoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => { if (c.Id == Guid.Empty) c.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>()))
            .Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name, Slug = c.Slug });

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CategoryDetailDto>.Success success)
        {
            success.Data.Slug.Should().Be(dto.Slug);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Success");
        }
        _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateCategoryAsync_DuplicateSlug_ReturnsDuplicateCategorySlugFailure()
    {
        // Arrange
        var dto = new CreateCategoryDto { Name = "Dup", Slug = "dup" };
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug)).ReturnsAsync(false);

        // Act
        var result = await _service.CreateCategoryAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CategoryDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.DuplicateCategorySlug);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_ValidData_ReturnsUpdatedCategory()
    {
        // Arrange
        var existing = TestDataFactory.CreateCategory("OldCat", slug: "old-cat");
        var dto = new UpdateCategoryDto { Name = "Updated", Slug = "updated-cat" };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<bool>())).ReturnsAsync(existing);
        _mockCategoryRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, existing.Id)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map(dto, existing)).Callback(() =>
        {
            existing.Name = dto.Name!;
            existing.Slug = dto.Slug!;
        });
        _mockCategoryRepository.Setup(r => r.Update(existing));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<CategoryDetailDto>(It.IsAny<Category>())).Returns((Category c) => new CategoryDetailDto { Id = c.Id, Name = c.Name, Slug = c.Slug });

        // Act
        var result = await _service.UpdateCategoryAsync(existing.Id, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result is Result<CategoryDetailDto>.Success success)
        {
            success.Data.Name.Should().Be("Updated");
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Success");
        }
        _mockCategoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_NonExistentId_ReturnsCategoryNotFoundFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateCategoryDto { Name = "Nope" };
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Category?)null);

        // Act
        var result = await _service.UpdateCategoryAsync(id, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<CategoryDetailDto>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CategoryNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<CategoryDetailDto>.Failure");
        }
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_ExistingId_DeletesCategory()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<bool>())).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(0);
        _mockCategoryRepository.Setup(r => r.Delete(category));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteCategoryAsync(category.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCategoryRepository.Verify(r => r.Delete(It.IsAny<Category>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_NonExistentId_ReturnsCategoryNotFoundFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Category?)null);

        // Act
        var result = await _service.DeleteCategoryAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CategoryNotFound);
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_CategoryWithProducts_ReturnsCategoryHasProductsFailure()
    {
        // Arrange
        var category = TestDataFactory.CreateCategory();
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<bool>())).ReturnsAsync(category);
        _mockCategoryRepository.Setup(r => r.GetProductCountAsync(category.Id)).ReturnsAsync(3);

        // Act
        var result = await _service.DeleteCategoryAsync(category.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        if (result is Result<ECommerce.Core.Results.Unit>.Failure failure)
        {
            failure.Code.Should().Be(ErrorCodes.CategoryHasProducts);
        }
        else
        {
            Assert.Fail("Expected Result<Unit>.Failure");
        }
    }
}
