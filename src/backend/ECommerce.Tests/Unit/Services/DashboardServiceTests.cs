using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.Services;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Application.DTOs.Dashboard;
using FluentAssertions;
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
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object);

            // Act
            var result = await service.GetDashboardStatsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(42);
            result.TotalRevenue.Should().Be(1234.56m);
            result.TotalCustomers.Should().Be(17);
            result.TotalProducts.Should().Be(100);

            result.OrdersTrend.Should().HaveCount(2);
            result.OrdersTrend[0].Date.Should().Be(today.ToString("yyyy-MM-dd"));
            result.OrdersTrend[0].Count.Should().Be(8);

            result.RevenueTrend.Should().HaveCount(2);
            result.RevenueTrend[0].Date.Should().Be(today.ToString("yyyy-MM-dd"));
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
            var service = new DashboardService(mockUnitOfWork.Object, mockMapper.Object);

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
    }
}
