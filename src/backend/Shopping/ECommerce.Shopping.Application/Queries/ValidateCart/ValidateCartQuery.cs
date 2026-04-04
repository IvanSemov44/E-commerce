using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Application.Queries.ValidateCart;

public record ValidateCartQuery(
    Guid  CartId,
    Guid? RequestingUserId,
    bool  IsAdmin
) : IRequest<Result>;