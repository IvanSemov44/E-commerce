namespace ECommerce.Identity.Application.Queries.GetUserPreferences;

public record GetUserPreferencesQuery(Guid UserId) : IRequest<Result<UserPreferencesDto>>;
