using ECommerce.Identity.Application.DTOs;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
using MediatR;

namespace ECommerce.Identity.Application.Commands.Login;

public record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthTokenDto>>, ITransactionalCommand;
