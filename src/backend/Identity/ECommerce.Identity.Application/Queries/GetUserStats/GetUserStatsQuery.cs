using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Queries.GetUserStats;

public record GetUserStatsQuery : IRequest<Result<UserStatsDto>>;
