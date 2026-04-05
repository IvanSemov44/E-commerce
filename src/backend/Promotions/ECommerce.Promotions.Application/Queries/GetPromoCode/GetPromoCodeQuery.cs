using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.GetPromoCode;

public record GetPromoCodeQuery(Guid Id) : IRequest<Result<PromoCodeDto>>;