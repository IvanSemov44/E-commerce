namespace ECommerce.Shopping.Application.Commands.MergeCart;

public class MergeCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<MergeCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(MergeCartCommand command, CancellationToken ct)
    {
        var sessionCart = await _carts.GetBySessionIdAsync(command.SessionId, ct);
        if (sessionCart is null || sessionCart.IsEmpty)
            return Result<CartDto>.Ok(Cart.Create(command.UserId).ToDto());

        var userCart = await _carts.GetOrCreateForUserAsync(command.UserId, ct);

        foreach (var item in sessionCart.Items)
        {
            var addResult = userCart.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.Currency);
            if (!addResult.IsSuccess)
                return Result<CartDto>.Fail(addResult.GetErrorOrThrow());
        }

        await _carts.DeleteAsync(sessionCart, ct);

        return Result<CartDto>.Ok(userCart.ToDto());
    }
}
