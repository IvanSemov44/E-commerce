using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public sealed class DeactivatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<DeactivatePromoCodeCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeactivatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result<Unit>.Fail(PromotionsErrors.PromoNotFound);

        promoCode.Deactivate();

        await repository.UpsertAsync(promoCode, cancellationToken);

        return Result<Unit>.Ok(Unit.Value);
    }
}
