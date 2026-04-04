using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    Guid UserId,
    Guid CartItemId
) : IRequest<Result<CartDto>>, ITransactionalCommand;