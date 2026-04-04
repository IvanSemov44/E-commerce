using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Application.DTOs;

namespace ECommerce.Shopping.Application.Queries.GetCart;

public record GetCartQuery(Guid UserId) : IRequest<Result<CartDto>>;