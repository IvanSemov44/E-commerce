using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

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
) : IRequest<Result<Guid>>, ITransactionalCommand;
