using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;

    public DashboardService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        // Get total counts
        var totalOrders = await _orderRepository.GetTotalOrdersCountAsync();
        var totalRevenue = await _orderRepository.GetTotalRevenueAsync();
        var totalCustomers = await _userRepository.GetCustomersCountAsync();
        var totalProducts = await _productRepository.GetActiveProductsCountAsync();

        // Get trends for last 30 days
        var ordersTrendDict = await _orderRepository.GetOrdersTrendAsync(30);
        var revenueTrendDict = await _orderRepository.GetRevenueTrendAsync(30);

        // Convert to DTOs
        var ordersTrend = ordersTrendDict
            .OrderByDescending(x => x.Key)
            .Take(30)
            .Select(x => new OrderTrendDto
            {
                Date = x.Key.ToString("yyyy-MM-dd"),
                Count = x.Value
            })
            .ToList();

        var revenueTrend = revenueTrendDict
            .OrderByDescending(x => x.Key)
            .Take(30)
            .Select(x => new RevenueTrendDto
            {
                Date = x.Key.ToString("yyyy-MM-dd"),
                Amount = x.Value
            })
            .ToList();

        return new DashboardStatsDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            OrdersTrend = ordersTrend,
            RevenueTrend = revenueTrend
        };
    }
}
