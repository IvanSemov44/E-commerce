using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public record ClearWishlistCommand(Guid UserId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;