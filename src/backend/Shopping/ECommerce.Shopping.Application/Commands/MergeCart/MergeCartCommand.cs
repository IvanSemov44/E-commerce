
namespace ECommerce.Shopping.Application.Commands.MergeCart;

/// <summary>
/// Merges a session-based cart with a user's authenticated cart.
/// Called when a user logs in after shopping anonymously.
/// Items from the session cart are added to the user cart (idempotent).
/// </summary>
public record MergeCartCommand(Guid UserId, string SessionId)
    : IRequest<Result<CartDto>>, ITransactionalCommand;
