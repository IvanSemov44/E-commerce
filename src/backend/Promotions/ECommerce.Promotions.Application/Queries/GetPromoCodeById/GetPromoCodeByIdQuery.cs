using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.GetPromoCodeById;

public record GetPromoCodeByIdQuery(Guid Id) : IRequest<Result<PromoCodeDto>>;
