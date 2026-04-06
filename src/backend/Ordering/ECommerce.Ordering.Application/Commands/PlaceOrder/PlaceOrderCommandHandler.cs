using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Domain;
using ECommerce.Ordering.Application.DTOs;
using ECommerce.Ordering.Application.Mapping;
using ECommerce.Ordering.Application.Interfaces;
using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.Interfaces;
using ECommerce.Ordering.Domain.ValueObjects;

namespace ECommerce.Ordering.Application.Commands.PlaceOrder;

public class PlaceOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork uow,
    IProductCatalogReader productReader,
    IPromoCodeLookup promoCodeLookup,
    IShippingAddressReader shippingAddressReader,
    ICurrentUserService currentUser,
    IOrderIntegrationEventPublisher orderIntegrationEventPublisher
) : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<OrderDto>.Fail(new DomainError("UNAUTHORIZED", "User not authenticated."));

        var productIds = command.CartItems.Select(x => x.ProductId).ToList();
        var products = await productReader.GetProductsAsync(productIds, ct);

        if (products.Count != productIds.Count)
            return Result<OrderDto>.Fail(OrderingApplicationErrors.ProductsUnavailable);

        var orderItems = command.CartItems.Select(ci =>
        {
            var p = products.First(x => x.ProductId == ci.ProductId);
            return new OrderItemData(p.ProductId, p.ProductName, p.UnitPrice, ci.Quantity, p.ImageUrl);
        }).ToList();

        var subtotal = orderItems.Sum(i => i.UnitPrice * i.Quantity);

        decimal discountAmount = 0;
        Guid? promoCodeId = null;

        if (!string.IsNullOrEmpty(command.PromoCode))
        {
            var promoResult = await promoCodeLookup.GetPromoCodeAsync(command.PromoCode, ct);
            if (promoResult is null)
                return Result<OrderDto>.Fail(OrderingApplicationErrors.PromoCodeNotFound);

            var (discount, promoId) = promoResult.Value;
            discountAmount = discount;
            promoCodeId = promoId;
        }

        var address = await shippingAddressReader.GetShippingAddressAsync(userId.Value, command.ShippingAddressId, ct);
        if (address is null)
            return Result<OrderDto>.Fail(OrderingApplicationErrors.AddressNotFound);

        var shippingAddress = ShippingAddress.Create(
            address.Street, address.City, address.Country, address.PostalCode);

        var payment = PaymentInfo.Create(
            command.PaymentReference,
            command.PaymentMethod,
            subtotal - discountAmount + command.ShippingCost + command.TaxAmount,
            DateTime.UtcNow);

        if (!payment.IsSuccess)
            return Result<OrderDto>.Fail(payment.GetErrorOrThrow());

        var orderResult = Order.Place(
            userId.Value,
            shippingAddress,
            orderItems,
            command.ShippingCost,
            command.TaxAmount,
            payment.GetDataOrThrow(),
            discountAmount,
            promoCodeId);

        if (!orderResult.IsSuccess)
            return Result<OrderDto>.Fail(orderResult.GetErrorOrThrow());

        var order = orderResult.GetDataOrThrow();

        await orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);

        await orderIntegrationEventPublisher.PublishOrderPlacedAsync(
            order.Id,
            order.UserId,
            order.Items.Select(x => x.ProductId).ToArray(),
            order.Total,
            ct);

        await uow.SaveChangesAsync(ct);

        return Result<OrderDto>.Ok(order.ToDto());
    }
}

public static class OrderingApplicationErrors
{
    public static readonly DomainError ProductsUnavailable = new("PRODUCTS_UNAVAILABLE", "One or more products are unavailable.");
    public static readonly DomainError PromoCodeNotFound = new("PROMO_CODE_NOT_FOUND", "Promo code not found.");
    public static readonly DomainError PromoCodeInvalid = new("PROMO_CODE_INVALID", "Promo code is invalid or expired.");
    public static readonly DomainError AddressNotFound = new("ADDRESS_NOT_FOUND", "Shipping address not found.");
}
