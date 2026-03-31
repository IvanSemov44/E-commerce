using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
