using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public sealed class DeactivatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<DeactivatePromoCodeCommand, Result>
{
    public async Task<Result> Handle(DeactivatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result.Fail(PromotionsErrors.PromoNotFound);

        promoCode.Deactivate();

        await repository.UpsertAsync(promoCode, cancellationToken);

        return Result.Ok();
    }
}
