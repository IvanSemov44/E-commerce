namespace ECommerce.Shopping.Application.Commands.ClearCart;

public class ClearCartCommandHandler(
    ICartRepository _carts
) : IRequestHandler<ClearCartCommand, Result>
{
    public async Task<Result> Handle(ClearCartCommand command, CancellationToken ct)
    {
        var cart = await _carts.ResolveAsync(command.UserId, command.SessionId, ct);
        cart?.Clear();
        return Result.Ok();
    }
}
