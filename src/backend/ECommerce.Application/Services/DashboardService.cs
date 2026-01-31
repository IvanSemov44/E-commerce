using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        // Get total counts
        var totalOrders = await _unitOfWork.Orders.GetTotalOrdersCountAsync();
        var totalRevenue = await _unitOfWork.Orders.GetTotalRevenueAsync();
        var totalCustomers = await _unitOfWork.Users.GetCustomersCountAsync();
        var totalProducts = await _unitOfWork.Products.GetActiveProductsCountAsync();

        // Get trends for last 30 days
        var ordersTrendDict = await _unitOfWork.Orders.GetOrdersTrendAsync(30);
        var revenueTrendDict = await _unitOfWork.Orders.GetRevenueTrendAsync(30);

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
