using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Queries.ValidatePromoCode;

public record ValidatePromoCodeQuery(
    string Code,
    decimal OrderAmount
) : IRequest<Result<ValidatePromoCodeResultDto>>;
