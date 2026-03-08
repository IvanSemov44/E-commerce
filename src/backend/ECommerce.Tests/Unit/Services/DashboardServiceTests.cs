using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ECommerce.Application.Services;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Application.DTOs.Dashboard;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ECommerce.Tests.Helpers;

namespace ECommerce.Tests.Unit.Services
{
    [TestClass]
    public class DashboardServiceTests
    {
        [TestMethod]
        public async Task GetDashboardStatsAsync_ReturnsExpectedTotalsAndTrends()
        {
            // Arrange
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(42);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(1234.56m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(17);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(100);

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var ordersTrend = new Dictionary<DateTime, int>
            {
                [yesterday] = 5,
                [today] = 8
            };

            var revenueTrend = new Dictionary<DateTime, decimal>
            {
                [yesterday] = 50.5m,
                [today] = 80m
            };

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(ordersTrend);
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(revenueTrend);

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(42);
            result.TotalRevenue.Should().Be(1234.56m);
            result.TotalCustomers.Should().Be(17);
            result.TotalProducts.Should().Be(100);

            result.OrdersTrend.Should().HaveCount(2);
            result.OrdersTrend[0].Date.Should().Be(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            result.OrdersTrend[0].Count.Should().Be(8);

            result.RevenueTrend.Should().HaveCount(2);
            result.RevenueTrend[0].Date.Should().Be(today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            result.RevenueTrend[0].Amount.Should().Be(80m);
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_HandlesEmptyTrends()
        {
            // Arrange
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(0);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(0m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(0);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(0);

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, int>());
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, decimal>());

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(0);
            result.TotalRevenue.Should().Be(0m);
            result.TotalCustomers.Should().Be(0);
            result.TotalProducts.Should().Be(0);
            result.OrdersTrend.Should().BeEmpty();
            result.RevenueTrend.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_WithLargeNumbers_CalculatesCorrectly()
        {
            // Arrange
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(10000);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(999999.99m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(5000);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(2500);

            var ordersTrend = new Dictionary<DateTime, int>();
            var revenueTrend = new Dictionary<DateTime, decimal>();
            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                ordersTrend[date] = 100 + i;
                revenueTrend[date] = 1000m + (i * 10);
            }

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(ordersTrend);
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(revenueTrend);

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(10000);
            result.TotalRevenue.Should().Be(999999.99m);
            result.TotalCustomers.Should().Be(5000);
            result.TotalProducts.Should().Be(2500);
            result.OrdersTrend.Should().HaveCount(30);
            result.RevenueTrend.Should().HaveCount(30);
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_OrdersTrendOrdering_IsDescendingByDate()
        {
            // Arrange
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(10);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(100m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(5);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(10);

            var today = DateTime.UtcNow.Date;
            var ordersTrend = new Dictionary<DateTime, int>
            {
                [today.AddDays(-2)] = 5,
                [today.AddDays(-1)] = 10,
                [today] = 15
            };

            var revenueTrend = new Dictionary<DateTime, decimal>
            {
                [today.AddDays(-2)] = 50m,
                [today.AddDays(-1)] = 100m,
                [today] = 150m
            };

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(ordersTrend);
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(revenueTrend);

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert
            result.OrdersTrend.Should().HaveCount(3);
            // Verify descending order - most recent first
            result.OrdersTrend[0].Count.Should().Be(15);  // Today
            result.OrdersTrend[1].Count.Should().Be(10);  // Yesterday
            result.OrdersTrend[2].Count.Should().Be(5);   // Two days ago
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_CancellationToken_IsIgnoredWhenNotRequested()
        {
            // Arrange - Test that cancellation token is handled even when not cancelled
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10);
            orderRepo.Setup(r => r.GetTotalRevenueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(100m);
            userRepo.Setup(r => r.GetCustomersCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
            productRepo.Setup(r => r.GetActiveProductsCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(50);
            orderRepo.Setup(r => r.GetOrdersTrendAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<DateTime, int>());
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<DateTime, decimal>());

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            var cts = new CancellationTokenSource();

            // Act
            var result = await service.GetDashboardStatsAsync(cts.Token);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(10);
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_ReturnsConsistentData()
        {
            // Arrange - Call the service twice and verify consistent results
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(50);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(5000m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(100);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(200);

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, int>());
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, decimal>());

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result1 = await service.GetDashboardStatsAsync();
            var result2 = await service.GetDashboardStatsAsync();

            // Assert - Both results should be identical
            result1.TotalOrders.Should().Be(result2.TotalOrders);
            result1.TotalRevenue.Should().Be(result2.TotalRevenue);
            result1.TotalCustomers.Should().Be(result2.TotalCustomers);
            result1.TotalProducts.Should().Be(result2.TotalProducts);
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_RepositoryCallsAreInvoked()
        {
            // Arrange
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(25);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(2500m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(50);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(150);

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, int>());
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(new Dictionary<DateTime, decimal>());

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            await service.GetDashboardStatsAsync();

            // Assert - Verify all repository methods were called exactly once
            orderRepo.Verify(r => r.GetTotalOrdersCountAsync(It.IsAny<CancellationToken>()), Times.Once);
            orderRepo.Verify(r => r.GetTotalRevenueAsync(It.IsAny<CancellationToken>()), Times.Once);
            userRepo.Verify(r => r.GetCustomersCountAsync(It.IsAny<CancellationToken>()), Times.Once);
            productRepo.Verify(r => r.GetActiveProductsCountAsync(It.IsAny<CancellationToken>()), Times.Once);
            orderRepo.Verify(r => r.GetOrdersTrendAsync(30, It.IsAny<CancellationToken>()), Times.Once);
            orderRepo.Verify(r => r.GetRevenueTrendAsync(30, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetDashboardStatsAsync_ExceedsMaxTrendLimit_LimitedTo30()
        {
            // Arrange - Create trends with more than 30 entries
            var orderRepo = new Mock<IOrderRepository>();
            var userRepo = new Mock<IUserRepository>();
            var productRepo = new Mock<IProductRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWork.Setup(u => u.Orders).Returns(orderRepo.Object);
            mockUnitOfWork.Setup(u => u.Users).Returns(userRepo.Object);
            mockUnitOfWork.Setup(u => u.Products).Returns(productRepo.Object);

            orderRepo.Setup(r => r.GetTotalOrdersCountAsync()).ReturnsAsync(100);
            orderRepo.Setup(r => r.GetTotalRevenueAsync()).ReturnsAsync(10000m);
            userRepo.Setup(r => r.GetCustomersCountAsync()).ReturnsAsync(200);
            productRepo.Setup(r => r.GetActiveProductsCountAsync()).ReturnsAsync(500);

            var ordersTrend = new Dictionary<DateTime, int>();
            var revenueTrend = new Dictionary<DateTime, decimal>();
            for (int i = 0; i < 50; i++)  // Create 50 days of data
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                ordersTrend[date] = 100 + i;
                revenueTrend[date] = 1000m + (i * 10);
            }

            orderRepo.Setup(r => r.GetOrdersTrendAsync(30)).ReturnsAsync(ordersTrend);
            orderRepo.Setup(r => r.GetRevenueTrendAsync(30)).ReturnsAsync(revenueTrend);

            var mockMapper = MockHelpers.CreateMockMapper();
            var mockLogger = new Mock<ILogger<DashboardService>>();
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object, mockLogger.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert - Ensure results are limited to 30 items
            result.OrdersTrend.Should().HaveCount(30);
            result.RevenueTrend.Should().HaveCount(30);
        }
    }
}
