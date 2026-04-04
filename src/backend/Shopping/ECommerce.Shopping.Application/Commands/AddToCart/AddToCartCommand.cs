using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.AddToCart;

public record AddToCartCommand(
    Guid UserId,
    Guid ProductId,
    int  Quantity
) : IRequest<Result<CartDto>>, ITransactionalCommand;