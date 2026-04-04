using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;