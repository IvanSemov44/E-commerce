using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CoreOrder = ECommerce.SharedKernel.Entities.Order;
using CoreOrderItem = ECommerce.SharedKernel.Entities.OrderItem;
using SharedOrderStatus = ECommerce.SharedKernel.Enums.OrderStatus;
using SharedPaymentStatus = ECommerce.SharedKernel.Enums.PaymentStatus;

namespace ECommerce.Ordering.Infrastructure.Persistence.Repositories;

public class OrderRepository(OrderingDbContext _db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? null : MapToDomain(order, true);
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? null : MapToDomain(order, true);
    }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .ToListAsync(ct);

        return orders.Select(o => MapToDomain(o, true)).ToList();
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ToListAsync(ct);

        return orders.Select(o => MapToDomain(o, true)).ToList();
    }

    public Task<int> GetTotalOrdersCountAsync(CancellationToken ct = default)
        => _db.Orders.AsNoTracking().CountAsync(ct);

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
        => await _db.Orders
            .AsNoTracking()
            .Where(o => o.PaymentStatus == SharedPaymentStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

    public async Task<Dictionary<DateTime, int>> GetOrdersTrendAsync(int days, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return data.ToDictionary(x => x.Date, x => x.Count);
    }

    public async Task<Dictionary<DateTime, decimal>> GetRevenueTrendAsync(int days, CancellationToken ct = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.PaymentStatus == SharedPaymentStatus.Paid)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        return data.ToDictionary(x => x.Date, x => x.Amount);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        var entity = new CoreOrder
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            ShippingAmount = order.ShippingCost,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.Total,
            Status = SharedOrderStatus.Pending,
            PaymentStatus = SharedPaymentStatus.Pending,
            RowVersion = Array.Empty<byte>()
        };

        await _db.Orders.AddAsync(entity, ct);

        foreach (var item in order.Items)
        {
            _db.OrderItems.Add(new CoreOrderItem
            {
                Id = item.Id,
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                ProductImageUrl = item.ProductImageUrl
            });
        }
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        var existing = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

        if (existing is null) return;

        existing.Subtotal = order.Subtotal;
        existing.DiscountAmount = order.DiscountAmount;
        existing.ShippingAmount = order.ShippingCost;
        existing.TaxAmount = order.TaxAmount;
        existing.TotalAmount = order.Total;

        var domainStatus = order.Status.Name;
        if (Enum.TryParse<SharedOrderStatus>(domainStatus, true, out var coreStatus))
            existing.Status = coreStatus;
    }

    private static Order MapToDomain(CoreOrder order, bool withItems)
    {
        _ = withItems;

        var shippingAddress = ShippingAddress.Create(
            order.ShippingAddress?.StreetLine1 ?? "",
            order.ShippingAddress?.City ?? "",
            order.ShippingAddress?.Country ?? "",
            order.ShippingAddress?.PostalCode);

        var payment = PaymentInfo.Create(
            order.PaymentIntentId ?? "unknown",
            order.PaymentMethod ?? "unknown",
            order.TotalAmount,
            DateTime.UtcNow).GetDataOrThrow();

        var items = order.Items.Select(i => new OrderItemData(
            i.ProductId ?? Guid.Empty,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.ProductImageUrl)).ToList();

        var result = Order.Place(
            order.UserId ?? Guid.Empty,
            shippingAddress,
            items,
            order.ShippingAmount,
            order.TaxAmount,
            payment,
            order.DiscountAmount,
            order.PromoCodeId);

        if (!result.IsSuccess)
            throw new InvalidOperationException("Failed to map order from Core");

        var domainOrder = result.GetDataOrThrow();

        if (Enum.TryParse<SharedOrderStatus>(order.Status.ToString(), true, out var status))
        {
            var statusName = status.ToString();
            var domainStatus = OrderStatus.FromName(statusName);
            var statusField = typeof(Order).GetProperty(nameof(Order.Status));
            statusField?.SetValue(domainOrder, domainStatus);
        }

        return domainOrder;
    }
}
