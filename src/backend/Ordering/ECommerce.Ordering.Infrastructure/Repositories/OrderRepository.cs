using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Domain.ValueObjects;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CoreOrder = ECommerce.Core.Entities.Order;
using CoreOrderItem = ECommerce.Core.Entities.OrderItem;

namespace ECommerce.Ordering.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext _db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? null : MapToDomain(order, false);
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
            Status = Core.Enums.OrderStatus.Pending,
            PaymentStatus = Core.Enums.PaymentStatus.Pending,
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

        await _db.SaveChangesAsync(ct);
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
        if (Enum.TryParse<Core.Enums.OrderStatus>(domainStatus, true, out var coreStatus))
            existing.Status = coreStatus;

        await _db.SaveChangesAsync(ct);
    }

    private static Order MapToDomain(CoreOrder order, bool withItems)
    {
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

        if (Enum.TryParse<Core.Enums.OrderStatus>(order.Status.ToString(), true, out var status))
        {
            var statusName = status.ToString();
            var domainStatus = OrderStatus.FromName(statusName);
            var statusField = typeof(Order).GetProperty(nameof(Order.Status));
            statusField?.SetValue(domainOrder, domainStatus);
        }

        return domainOrder;
    }
}
