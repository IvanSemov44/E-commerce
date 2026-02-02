using AutoMapper;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class InventoryServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IRepository<InventoryLog>> _mockInventoryLogRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<InventoryService>> _mockLogger = null!;
    private Mock<IMapper> _mockMapper = null!;
    private InventoryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockInventoryLogRepository = new Mock<IRepository<InventoryLog>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<InventoryService>>();
        _mockMapper = MockHelpers.CreateMockMapper();

        _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(u => u.InventoryLogs).Returns(_mockInventoryLogRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _service = new InventoryService(
            _mockUnitOfWork.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    #region ReduceStockAsync Tests

    [TestMethod]
    public async Task ReduceStockAsync_ValidReduction_ReducesStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 100);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.ReduceStockAsync(productId, 30, "Order fulfillment");

        // Assert
        product.StockQuantity.Should().Be(70);
        _mockProductRepository.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ReduceStockAsync_ZeroQuantity_ThrowsInvalidQuantityException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.ReduceStockAsync(productId, 0, "test"))
            .Should().ThrowAsync<InvalidQuantityException>()
            .WithMessage("*Quantity must be positive*");
    }

    [TestMethod]
    public async Task ReduceStockAsync_NegativeQuantity_ThrowsInvalidQuantityException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.ReduceStockAsync(productId, -10, "test"))
            .Should().ThrowAsync<InvalidQuantityException>();
    }

    [TestMethod]
    public async Task ReduceStockAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await _service.Invoking(s => s.ReduceStockAsync(productId, 10, "test"))
            .Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task ReduceStockAsync_InsufficientStock_ThrowsInsufficientStockException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 5);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act & Assert
        await _service.Invoking(s => s.ReduceStockAsync(productId, 10, "test"))
            .Should().ThrowAsync<InsufficientStockException>();
    }

    [TestMethod]
    public async Task ReduceStockAsync_CreatesInventoryLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 100);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.ReduceStockAsync(productId, 25, "Test reduction", referenceId, userId);

        // Assert
        _mockInventoryLogRepository.Verify(
            r => r.AddAsync(It.Is<InventoryLog>(log =>
                log.ProductId == productId &&
                log.QuantityChange == -25 &&
                log.Reason == "Test reduction" &&
                log.ReferenceId == referenceId &&
                log.CreatedByUserId == userId
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region IncreaseStockAsync Tests

    [TestMethod]
    public async Task IncreaseStockAsync_ValidIncrease_IncreasesStock()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 50);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.IncreaseStockAsync(productId, 30, "Restocking");

        // Assert
        product.StockQuantity.Should().Be(80);
        _mockProductRepository.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IncreaseStockAsync_ZeroQuantity_ThrowsInvalidQuantityException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.IncreaseStockAsync(productId, 0, "test"))
            .Should().ThrowAsync<InvalidQuantityException>();
    }

    [TestMethod]
    public async Task IncreaseStockAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await _service.Invoking(s => s.IncreaseStockAsync(productId, 10, "test"))
            .Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task IncreaseStockAsync_CreatesInventoryLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 50);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.IncreaseStockAsync(productId, 20, "New shipment");

        // Assert
        _mockInventoryLogRepository.Verify(
            r => r.AddAsync(It.Is<InventoryLog>(log =>
                log.ProductId == productId &&
                log.QuantityChange == 20 &&
                log.Reason == "New shipment"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region AdjustStockAsync Tests

    [TestMethod]
    public async Task AdjustStockAsync_ValidAdjustment_SetsNewQuantity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 50);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.AdjustStockAsync(productId, 75, "Inventory count correction");

        // Assert
        product.StockQuantity.Should().Be(75);
    }

    [TestMethod]
    public async Task AdjustStockAsync_NegativeQuantity_ThrowsInvalidQuantityException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.AdjustStockAsync(productId, -10, "test"))
            .Should().ThrowAsync<InvalidQuantityException>()
            .WithMessage("*Quantity cannot be negative*");
    }

    [TestMethod]
    public async Task AdjustStockAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await _service.Invoking(s => s.AdjustStockAsync(productId, 100, "test"))
            .Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task AdjustStockAsync_ZeroQuantity_SetsStockToZero()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 50);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.AdjustStockAsync(productId, 0, "Out of stock");

        // Assert
        product.StockQuantity.Should().Be(0);
    }

    [TestMethod]
    public async Task AdjustStockAsync_CreatesInventoryLogWithCorrectChange()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(stock: 50);
        product.Id = productId;

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.AdjustStockAsync(productId, 80, "Adjustment");

        // Assert
        _mockInventoryLogRepository.Verify(
            r => r.AddAsync(It.Is<InventoryLog>(log =>
                log.ProductId == productId &&
                log.QuantityChange == 30 // 80 - 50 = +30
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CheckStockAvailabilityAsync Tests

    [TestMethod]
    public async Task CheckStockAvailabilityAsync_AllInStock_ReturnsAvailable()
    {
        // Arrange
        var product1 = TestDataFactory.CreateProduct(stock: 100);
        var product2 = TestDataFactory.CreateProduct(stock: 50);

        _mockProductRepository.Setup(r => r.GetByIdAsync(product1.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _mockProductRepository.Setup(r => r.GetByIdAsync(product2.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        var items = new List<StockCheckItemDto>
        {
            new() { ProductId = product1.Id, Quantity = 10 },
            new() { ProductId = product2.Id, Quantity = 5 }
        };

        // Act
        var result = await _service.CheckStockAvailabilityAsync(items);

        // Assert
        result.IsAvailable.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CheckStockAvailabilityAsync_InsufficientStock_ReturnsIssues()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stock: 5, name: "Test Product");

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var items = new List<StockCheckItemDto>
        {
            new() { ProductId = product.Id, Quantity = 10 }
        };

        // Act
        var result = await _service.CheckStockAvailabilityAsync(items);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Issues.Should().HaveCount(1);
        result.Issues[0].ProductId.Should().Be(product.Id);
        result.Issues[0].ProductName.Should().Be("Test Product");
        result.Issues[0].RequestedQuantity.Should().Be(10);
        result.Issues[0].AvailableQuantity.Should().Be(5);
    }

    [TestMethod]
    public async Task CheckStockAvailabilityAsync_ProductNotFound_ReturnsIssue()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var items = new List<StockCheckItemDto>
        {
            new() { ProductId = productId, Quantity = 5 }
        };

        // Act
        var result = await _service.CheckStockAvailabilityAsync(items);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Issues.Should().HaveCount(1);
        result.Issues[0].Message.Should().Be("Product not found");
    }

    [TestMethod]
    public async Task CheckStockAvailabilityAsync_OutOfStock_ShowsOutOfStockMessage()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stock: 0, name: "Out of Stock Product");

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var items = new List<StockCheckItemDto>
        {
            new() { ProductId = product.Id, Quantity = 1 }
        };

        // Act
        var result = await _service.CheckStockAvailabilityAsync(items);

        // Assert
        result.IsAvailable.Should().BeFalse();
        result.Issues[0].Message.Should().Be("Out of stock");
    }

    #endregion

    #region IsStockAvailableAsync Tests

    [TestMethod]
    public async Task IsStockAvailableAsync_SufficientStock_ReturnsTrue()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stock: 100);

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.IsStockAvailableAsync(product.Id, 50);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsStockAvailableAsync_InsufficientStock_ReturnsFalse()
    {
        // Arrange
        var product = TestDataFactory.CreateProduct(stock: 10);

        _mockProductRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.IsStockAvailableAsync(product.Id, 20);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsStockAvailableAsync_ProductNotFound_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.IsStockAvailableAsync(productId, 10);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetLowStockProductsAsync Tests

    [TestMethod]
    public async Task GetLowStockProductsAsync_ReturnsLowStockProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            TestDataFactory.CreateProduct(stock: 5, name: "Low Stock 1", isActive: true),
            TestDataFactory.CreateProduct(stock: 100, name: "Normal Stock", isActive: true),
            TestDataFactory.CreateProduct(stock: 2, name: "Low Stock 2", isActive: true)
        };

        // Set low stock thresholds
        products[0].LowStockThreshold = 10;
        products[1].LowStockThreshold = 10;
        products[2].LowStockThreshold = 10;

        _mockProductRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mockMapper.Setup(m => m.Map<LowStockAlertDto>(It.IsAny<Product>()))
            .Returns((Product p) => new LowStockAlertDto { ProductName = p.Name, CurrentStock = p.StockQuantity });

        // Act
        var result = await _service.GetLowStockProductsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.ProductName).Should().Contain("Low Stock 1").And.Contain("Low Stock 2");
    }

    [TestMethod]
    public async Task GetLowStockProductsAsync_ExcludesInactiveProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            TestDataFactory.CreateProduct(stock: 5, name: "Low Stock Active", isActive: true),
            TestDataFactory.CreateProduct(stock: 3, name: "Low Stock Inactive", isActive: false)
        };

        products[0].LowStockThreshold = 10;
        products[1].LowStockThreshold = 10;

        _mockProductRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mockMapper.Setup(m => m.Map<LowStockAlertDto>(It.IsAny<Product>()))
            .Returns((Product p) => new LowStockAlertDto { ProductName = p.Name });

        // Act
        var result = await _service.GetLowStockProductsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be("Low Stock Active");
    }

    #endregion
}
