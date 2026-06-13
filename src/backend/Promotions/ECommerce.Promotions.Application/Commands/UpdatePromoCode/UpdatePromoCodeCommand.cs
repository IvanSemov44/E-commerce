using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

public record UpdatePromoCodeCommand(
    Guid Id,
    bool? IsActive = null,
    string? DiscountType = null,
    decimal? DiscountValue = null,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null,
    int? MaxUses = null,
    decimal? MinimumOrderAmount = null,
    decimal? MaxDiscountAmount = null
) : IRequest<Result<Guid>>, ITransactionalCommand;
