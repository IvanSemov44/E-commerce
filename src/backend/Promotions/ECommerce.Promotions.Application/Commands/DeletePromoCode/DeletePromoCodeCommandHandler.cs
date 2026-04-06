using MediatR;
using ECommerce.Promotions.Application.Interfaces;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public sealed class DeletePromoCodeCommandHandler(
    IPromoCodeRepository repository,
    IPromoProjectionEventPublisher projectionEventPublisher)
    : IRequestHandler<DeletePromoCodeCommand, Result>
{
    public async Task<Result> Handle(DeletePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result.Fail(PromotionsErrors.PromoNotFound);

        await repository.DeleteAsync(promoCode, cancellationToken);

        await projectionEventPublisher.PublishPromoProjectionUpdatedAsync(
            promoCode.Id,
            promoCode.Code.Value,
            promoCode.Discount.Amount,
            promoCode.IsActive,
            true,
            cancellationToken);

        return Result.Ok();
    }
}
