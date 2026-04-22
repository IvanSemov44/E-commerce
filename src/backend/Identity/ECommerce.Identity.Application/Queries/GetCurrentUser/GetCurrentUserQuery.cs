namespace ECommerce.Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;
