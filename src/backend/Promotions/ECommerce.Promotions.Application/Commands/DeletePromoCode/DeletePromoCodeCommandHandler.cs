using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public sealed class DeletePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<DeletePromoCodeCommand, Result>
{
    public async Task<Result> Handle(DeletePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result.Fail(PromotionsErrors.PromoNotFound);

        promoCode.SoftDelete();

        return Result.Ok();
    }
}
