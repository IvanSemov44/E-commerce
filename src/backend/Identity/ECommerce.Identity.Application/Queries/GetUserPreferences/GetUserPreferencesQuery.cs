using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Queries.GetUserPreferences;

public record GetUserPreferencesQuery(Guid UserId) : IRequest<Result<UserPreferencesDto>>;
