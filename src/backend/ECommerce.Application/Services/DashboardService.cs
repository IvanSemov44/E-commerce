using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Core.Interfaces.Repositories;

namespace ECommerce.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DashboardService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
            .Select(x => _mapper.Map<ECommerce.Application.DTOs.Dashboard.OrderTrendDto>(x))
            .ToList();

        var revenueTrend = revenueTrendDict
            .OrderByDescending(x => x.Key)
            .Take(30)
            .Select(x => _mapper.Map<ECommerce.Application.DTOs.Dashboard.RevenueTrendDto>(x))
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
