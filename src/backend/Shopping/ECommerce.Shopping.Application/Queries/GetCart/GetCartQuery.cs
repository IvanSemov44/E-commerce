
namespace ECommerce.Shopping.Application.Queries.GetCart;

public record GetCartQuery(Guid? UserId, string? SessionId) : IRequest<Result<CartDto>>;