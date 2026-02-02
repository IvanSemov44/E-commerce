using AutoMapper;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private ProductService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = MockHelpers.CreateMockMapper();

        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);

        _service = new ProductService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    [TestMethod]
    public async Task GetProductsAsync_NoFilters_ReturnsPaginatedProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            TestDataFactory.CreateProduct(),
            TestDataFactory.CreateProduct()
        };

        _mockProductRepository.Setup(r => r.GetProductsWithFiltersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<bool?>(), It.IsAny<string?>()))
            .ReturnsAsync((products.AsEnumerable(), products.Count));

        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name });

        // Act
        var result = await _service.GetProductsAsync(new ProductQueryDto { Page = 1, PageSize = 10 });

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetProductBySlugAsync_ExistingSlug_ReturnsProduct()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(name: "Test Product", slug: "test-product");
        _mockProductRepository.Setup(r => r.GetBySlugAsync("test-product"))
            .ReturnsAsync(product);

        _mockMapper.Setup(m => m.Map<ProductDetailDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDetailDto { Id = p.Id, Name = p.Name, Slug = p.Slug });

        // Act
        var result = await _service.GetProductBySlugAsync("test-product");

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().Be("test-product");
    }

    [TestMethod]
    public async Task GetProductBySlugAsync_NonExistentSlug_ThrowsProductNotFoundException()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetBySlugAsync("missing"))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetProductBySlugAsync("missing");

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task CreateProductAsync_ValidData_CreatesProduct()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "New Prod", Slug = "new-prod", Price = 10 };

        _mockProductRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, It.IsAny<Guid?>()))
            .ReturnsAsync(true);

        _mockMapper.Setup(m => m.Map<Product>(It.IsAny<CreateProductDto>()))
            .Returns((CreateProductDto d) => new Product { Id = Guid.NewGuid(), Name = d.Name, Slug = d.Slug, Price = d.Price });

        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => { if (p.Id == Guid.Empty) p.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<ProductDetailDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDetailDto { Id = p.Id, Name = p.Name, Slug = p.Slug });

        // Act
        var result = await _service.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().Be(dto.Slug);
        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateProductAsync_DuplicateSlug_ThrowsDuplicateProductSlugException()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Dup", Slug = "dup", Price = 5 };

        _mockProductRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, It.IsAny<Guid?>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.CreateProductAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateProductSlugException>();
    }

    [TestMethod]
    public async Task UpdateProductAsync_ValidData_UpdatesProduct()
    {
        // Arrange
        var existing = TestDataFactory.CreateProduct(name: "Old", slug: "old-slug");
        var dto = new UpdateProductDto { Name = "Updated", Slug = "updated-slug", Price = 20 };

        _mockProductRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<bool>())).ReturnsAsync(existing);
        _mockProductRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, existing.Id)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map(dto, existing)).Callback(() =>
        {
            existing.Name = dto.Name!;
            existing.Slug = dto.Slug!;
            existing.Price = dto.Price;
        });

        _mockProductRepository.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mockMapper.Setup(m => m.Map<ProductDetailDto>(It.IsAny<Product>())).Returns((Product p) => new ProductDetailDto { Id = p.Id, Name = p.Name, Slug = p.Slug });

        // Act
        var result = await _service.UpdateProductAsync(existing.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated");
        _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductAsync_NonExistentId_ThrowsProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductDto { Name = "Nope" };
        _mockProductRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateProductAsync(id, dto);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task DeleteProductAsync_ExistingId_DeletesProduct()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct();
        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.DeleteAsync(product)).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.DeleteProductAsync(product.Id);

        // Assert
        _mockProductRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductAsync_NonExistentId_ThrowsProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.DeleteProductAsync(id);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ExistingId_ReturnsProduct()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(name: "ById Prod");
        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>())).ReturnsAsync(product);

        _mockMapper.Setup(m => m.Map<ProductDetailDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDetailDto { Id = p.Id, Name = p.Name, Slug = p.Slug });

        // Act
        var result = await _service.GetProductByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(product.Id);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_NonExistent_ThrowsProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<bool>())).ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetProductByIdAsync(id);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task GetFeaturedProductsAsync_ReturnsRequestedCount()
    {
        // Arrange
        var featured = new List<Product>
        {
            TestDataFactory.CreateProduct(name: "F1"),
            TestDataFactory.CreateProduct(name: "F2")
        };

        _mockProductRepository.Setup(r => r.GetFeaturedAsync(2)).ReturnsAsync(featured);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name });

        // Act
        var result = await _service.GetFeaturedProductsAsync(2);

        // Assert
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task SearchProductsAsync_WithQuery_ReturnsMatchingProducts()
    {
        // Arrange
        var all = new List<Product>
        {
            TestDataFactory.CreateProduct(name: "Alpha"),
            TestDataFactory.CreateProduct(name: "Beta Product"),
            TestDataFactory.CreateProduct(name: "Gamma")
        };

        _mockProductRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(all);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name });

        // Act
        var result = await _service.SearchProductsAsync("Beta");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Contain("Beta");
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_ReturnsProducts()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var products = new List<Product>
        {
            TestDataFactory.CreateProduct(name: "C1"),
            TestDataFactory.CreateProduct(name: "C2")
        };

        _mockProductRepository.Setup(r => r.GetByCategoryAsync(categoryId)).ReturnsAsync(products);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name });

        // Act
        var result = await _service.GetProductsByCategoryAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetProductsByPriceRangeAsync_ReturnsFilteredProducts()
    {
        // Arrange
        var all = new List<Product>
        {
            TestDataFactory.CreateProduct(name: "Cheap", price: 5),
            TestDataFactory.CreateProduct(name: "Mid", price: 50),
            TestDataFactory.CreateProduct(name: "Expensive", price: 200)
        };

        _mockProductRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(all);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });

        // Act
        var result = await _service.GetProductsByPriceRangeAsync(10, 100);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(p => p.Price >= 10 && p.Price <= 100);
    }

    [TestMethod]
    public async Task GetLowStockProductsAsync_ReturnsLowStockItems()
    {
        // Arrange
        var p1 = TestDataFactory.CreateProduct(name: "P1", price: 10);
        p1.StockQuantity = 2;
        p1.LowStockThreshold = 5;
        var p2 = TestDataFactory.CreateProduct(name: "P2", price: 20);
        p2.StockQuantity = 10;
        p2.LowStockThreshold = 5;

        var all = new List<Product> { p1, p2 };
        _mockProductRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(all);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns((Product p) => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });

        // Act
        var result = await _service.GetLowStockProductsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be(p1.Name);
    }

    [TestMethod]
    public async Task UpdateProductAsync_DuplicateSlug_ThrowsDuplicateProductSlugException()
    {
        // Arrange
        var existing = TestDataFactory.CreateProduct(name: "Old", slug: "old-slug");
        var dto = new UpdateProductDto { Name = "Updated", Slug = "taken-slug" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<bool>())).ReturnsAsync(existing);
        _mockProductRepository.Setup(r => r.IsSlugUniqueAsync(dto.Slug, existing.Id)).ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.UpdateProductAsync(existing.Id, dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateProductSlugException>();
    }
}
