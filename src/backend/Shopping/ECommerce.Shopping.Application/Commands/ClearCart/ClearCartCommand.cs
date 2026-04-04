using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.ClearCart;

public record ClearCartCommand(Guid? UserId)
    : IRequest<Result<CartDto>>, ITransactionalCommand;