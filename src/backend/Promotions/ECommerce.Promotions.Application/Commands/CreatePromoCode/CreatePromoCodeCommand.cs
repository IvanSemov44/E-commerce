using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Application.DTOs;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public record CreatePromoCodeCommand(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null,
    int? MaxUses = null,
    decimal? MinimumOrderAmount = null,
    decimal? MaxDiscountAmount = null
) : IRequest<Result<PromoCodeDto>>;