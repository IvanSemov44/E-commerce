using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;