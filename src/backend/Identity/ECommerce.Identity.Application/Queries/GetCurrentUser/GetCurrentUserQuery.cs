using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;
