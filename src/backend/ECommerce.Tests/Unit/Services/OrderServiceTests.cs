using AutoMapper;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.PromoCodes;
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
public class OrderServiceTests
{
    private Mock<IOrderRepository> _mockOrderRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IProductRepository> _mockProductRepository = null!;
    private Mock<IPromoCodeService> _mockPromoCodeService = null!;
    private Mock<IInventoryService> _mockInventoryService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<OrderService>> _mockLogger = null!;
    private OrderService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockPromoCodeService = new Mock<IPromoCodeService>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockMapper = MockHelpers.CreateMockMapper();
        _mockLogger = MockHelpers.CreateMockLogger<OrderService>();

        _service = new OrderService(
            _mockOrderRepository.Object,
            _mockUserRepository.Object,
            _mockProductRepository.Object,
            _mockPromoCodeService.Object,
            _mockInventoryService.Object,
            _mockEmailService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    #region CreateOrderAsync Tests

    [TestMethod]
    public async Task CreateOrderAsync_ValidOrder_CreatesOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        var dto = new CreateOrderDto
        {
            PaymentMethod = "CreditCard",
            ShippingAddress = new AddressDto
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US",
                Phone = "555-1234"
            },
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = "Test Product",
                    Price = 100,
                    Quantity = 2
                }
            }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockInventoryService.Setup(s => s.CheckStockAvailabilityAsync(It.IsAny<List<StockCheckItemDto>>()))
            .ReturnsAsync(new StockCheckResponse
            {
                IsAvailable = true,
                Issues = new List<StockIssueDto>()
            });

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) =>
            {
                order.Id = Guid.NewGuid();
                return order;
            });

        _mockOrderRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = Guid.NewGuid() });

        // Act
        var result = await _service.CreateOrderAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrderAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateOrderDto
        {
            PaymentMethod = "CreditCard",
            Items = new List<CreateOrderItemDto>()
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _service.CreateOrderAsync(userId, dto);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [TestMethod]
    public async Task CreateOrderAsync_WithPromoCode_AppliesDiscount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        var promoCode = TestDataFactory.CreatePromoCode("SAVE20", discountValue: 20);
        var dto = new CreateOrderDto
        {
            PaymentMethod = "CreditCard",
            PromoCode = "SAVE20",
            ShippingAddress = new AddressDto
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US",
                Phone = "555-1234"
            },
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = "Test Product",
                    Price = 100,
                    Quantity = 1
                }
            }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockInventoryService.Setup(s => s.CheckStockAvailabilityAsync(It.IsAny<List<StockCheckItemDto>>()))
            .ReturnsAsync(new StockCheckResponse
            {
                IsAvailable = true,
                Issues = new List<StockIssueDto>()
            });

        _mockPromoCodeService.Setup(s => s.ValidatePromoCodeAsync("SAVE20", It.IsAny<decimal>()))
            .ReturnsAsync(new ValidatePromoCodeDto
            {
                IsValid = true,
                DiscountAmount = 20,
                PromoCode = new PromoCodeDto { Id = promoCode.Id, Code = "SAVE20" }
            });

        Order capturedOrder = null!;

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .ReturnsAsync((Order order) =>
            {
                order.Id = Guid.NewGuid();
                return order;
            });

        _mockOrderRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = Guid.NewGuid() });

        // Act
        await _service.CreateOrderAsync(userId, dto);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder.DiscountAmount.Should().Be(20);
        capturedOrder.PromoCodeId.Should().Be(promoCode.Id);
        _mockPromoCodeService.Verify(s => s.IncrementUsedCountAsync(promoCode.Id), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrderAsync_InvalidPromoCode_ThrowsInvalidPromoCodeException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        var dto = new CreateOrderDto
        {
            PaymentMethod = "CreditCard",
            PromoCode = "INVALID",
            ShippingAddress = new AddressDto
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US",
                Phone = "555-1234"
            },
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = "Test Product",
                    Price = 100,
                    Quantity = 1
                }
            }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockInventoryService.Setup(s => s.CheckStockAvailabilityAsync(It.IsAny<List<StockCheckItemDto>>()))
            .ReturnsAsync(new StockCheckResponse
            {
                IsAvailable = true,
                Issues = new List<StockIssueDto>()
            });

        _mockPromoCodeService.Setup(s => s.ValidatePromoCodeAsync("INVALID", It.IsAny<decimal>()))
            .ReturnsAsync(new ValidatePromoCodeDto
            {
                IsValid = false,
                DiscountAmount = 0
            });

        // Act
        Func<Task> act = async () => await _service.CreateOrderAsync(userId, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidPromoCodeException>();
    }

    [TestMethod]
    public async Task CreateOrderAsync_InsufficientStock_ThrowsInsufficientStockException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.CreateUser();
        var dto = new CreateOrderDto
        {
            PaymentMethod = "CreditCard",
            ShippingAddress = new AddressDto
            {
                FirstName = "John",
                LastName = "Doe",
                StreetLine1 = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "US",
                Phone = "555-1234"
            },
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = "Test Product",
                    Price = 100,
                    Quantity = 10
                }
            }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockInventoryService.Setup(s => s.CheckStockAvailabilityAsync(It.IsAny<List<StockCheckItemDto>>()))
            .ReturnsAsync(new StockCheckResponse
            {
                IsAvailable = false,
                Issues = new List<StockIssueDto>
                {
                    new StockIssueDto { Message = "Insufficient stock for Test Product" }
                }
            });

        // Act
        Func<Task> act = async () => await _service.CreateOrderAsync(userId, dto);

        // Assert
        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    #endregion

    #region GetOrderByIdAsync Tests

    [TestMethod]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid());

        _mockOrderRepository.Setup(r => r.GetWithItemsAsync(order.Id))
            .ReturnsAsync(order);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = order.Id });

        // Act
        var result = await _service.GetOrderByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
    }

    [TestMethod]
    public async Task GetOrderByIdAsync_OrderNotFound_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(r => r.GetWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderByIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetOrderByNumberAsync Tests

    [TestMethod]
    public async Task GetOrderByNumberAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid(), orderNumber: "ORD-12345");

        _mockOrderRepository.Setup(r => r.GetByOrderNumberAsync("ORD-12345"))
            .ReturnsAsync(order);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = order.Id, OrderNumber = "ORD-12345" });

        // Act
        var result = await _service.GetOrderByNumberAsync("ORD-12345");

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("ORD-12345");
    }

    [TestMethod]
    public async Task GetOrderByNumberAsync_OrderNotFound_ReturnsNull()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByOrderNumberAsync("NONEXISTENT"))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.GetOrderByNumberAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateOrderStatusAsync Tests

    [TestMethod]
    public async Task UpdateOrderStatusAsync_ValidStatus_UpdatesOrder()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid());
        var newStatus = "Processing";

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = order.Id });

        // Act
        var result = await _service.UpdateOrderStatusAsync(order.Id, newStatus);

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ThrowsOrderNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateOrderStatusAsync(orderId, "Processing");

        // Assert
        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_InvalidStatus_ThrowsInvalidOrderStatusException()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid());

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _service.UpdateOrderStatusAsync(order.Id, "InvalidStatus");

        // Assert
        await act.Should().ThrowAsync<InvalidOrderStatusException>();
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_Shipped_SetsShippedAt()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid());

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockMapper.Setup(m => m.Map<OrderDetailDto>(It.IsAny<Order>()))
            .Returns(new OrderDetailDto { Id = order.Id });

        // Act
        await _service.UpdateOrderStatusAsync(order.Id, "Shipped");

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();
        order.ShippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region CancelOrderAsync Tests

    [TestMethod]
    public async Task CancelOrderAsync_ValidOrder_CancelsOrder()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid(), status: OrderStatus.Pending);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CancelOrderAsync(order.Id);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [TestMethod]
    public async Task CancelOrderAsync_OrderNotFound_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task CancelOrderAsync_AlreadyShipped_ThrowsInvalidOrderStatusException()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid(), status: OrderStatus.Shipped);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _service.CancelOrderAsync(order.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOrderStatusException>();
    }

    [TestMethod]
    public async Task CancelOrderAsync_AlreadyDelivered_ThrowsInvalidOrderStatusException()
    {
        // Arrange
        var order = TestDataFactory.CreateOrder(Guid.NewGuid(), status: OrderStatus.Delivered);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        Func<Task> act = async () => await _service.CancelOrderAsync(order.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOrderStatusException>();
    }

    #endregion

    #region GetUserOrdersAsync Tests

    [TestMethod]
    public async Task GetUserOrdersAsync_WithOrders_ReturnsPaginatedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orders = new List<Order>
        {
            TestDataFactory.CreateOrder(userId),
            TestDataFactory.CreateOrder(userId),
            TestDataFactory.CreateOrder(userId)
        };

        _mockOrderRepository.Setup(r => r.GetUserOrdersCountAsync(userId))
            .ReturnsAsync(3);

        _mockOrderRepository.Setup(r => r.GetUserOrdersAsync(userId, 0, 10))
            .ReturnsAsync(orders);

        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns((Order o) => new OrderDto { Id = o.Id });

        // Act
        var result = await _service.GetUserOrdersAsync(userId, page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    #endregion

    #region GetAllOrdersAsync Tests

    [TestMethod]
    public async Task GetAllOrdersAsync_WithOrders_ReturnsPaginatedResult()
    {
        // Arrange
        var orders = new List<Order>
        {
            TestDataFactory.CreateOrder(Guid.NewGuid()),
            TestDataFactory.CreateOrder(Guid.NewGuid()),
            TestDataFactory.CreateOrder(Guid.NewGuid()),
            TestDataFactory.CreateOrder(Guid.NewGuid()),
            TestDataFactory.CreateOrder(Guid.NewGuid())
        };

        _mockOrderRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(orders);

        _mockMapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns((Order o) => new OrderDto { Id = o.Id });

        // Act
        var result = await _service.GetAllOrdersAsync(page: 1, pageSize: 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    #endregion
}
