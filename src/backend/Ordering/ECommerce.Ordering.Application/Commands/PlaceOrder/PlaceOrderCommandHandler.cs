using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.ValueObjects;

namespace ECommerce.Ordering.Application.Commands.PlaceOrder;

public class PlaceOrderCommandHandler(
    IOrderRepository orders,
    IProductCatalogReader productReader,
    IPromoCodeLookup promoCodeLookup,
    IShippingAddressReader shippingAddressReader,
    ICurrentUserService currentUser
) : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserIdOrNull;
        if (userId is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.Unauthorized);

        var productIds = command.CartItems.Select(x => x.ProductId).Distinct().ToList();
        var products = await productReader.GetProductsAsync(productIds, ct);

        if (products.Count != productIds.Count)
            return Result<Guid>.Fail(OrderingApplicationErrors.ProductsUnavailable);

        var orderItems = command.CartItems.ConvertAll(ci =>
        {
            var p = products.First(x => x.ProductId == ci.ProductId);
            return new OrderItemData(p.ProductId, p.ProductName, p.UnitPrice, ci.Quantity, p.ImageUrl);
        });

        decimal discountAmount = 0;
        Guid? promoCodeId = null;

        if (!string.IsNullOrEmpty(command.PromoCode))
        {
            var promoResult = await promoCodeLookup.GetPromoCodeAsync(command.PromoCode, ct);
            if (promoResult is null)
                return Result<Guid>.Fail(OrderingApplicationErrors.PromoCodeNotFound);

            var (discount, promoId) = promoResult.Value;
            discountAmount = discount;
            promoCodeId = promoId;
        }

        var address = await shippingAddressReader.GetShippingAddressAsync(userId.Value, command.ShippingAddressId, ct);
        if (address is null)
            return Result<Guid>.Fail(OrderingApplicationErrors.AddressNotFound);

        var shippingAddress = ShippingAddress.Create(
            address.Street, address.City, address.Country, address.PostalCode);

        var orderResult = Order.Place(
            userId.Value,
            shippingAddress,
            orderItems,
            command.ShippingCost,
            command.TaxAmount,
            command.PaymentReference,
            command.PaymentMethod,
            discountAmount,
            promoCodeId);

        if (!orderResult.IsSuccess)
            return Result<Guid>.Fail(orderResult.GetErrorOrThrow());

        var order = orderResult.GetDataOrThrow();

        await orders.AddAsync(order, ct);

        return Result<Guid>.Ok(order.Id);
    }
}
