using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;

namespace ECommerce.Promotions.Application.Commands.UpdatePromoCode;

public sealed class UpdatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<UpdatePromoCodeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (promoCode is null)
            return Result<Guid>.Fail(PromotionsErrors.PromoNotFound);

        var updateResult = promoCode.Update(
            discountType: command.DiscountType,
            discountValue: command.DiscountValue,
            validFrom: command.ValidFrom,
            validUntil: command.ValidUntil,
            maxUses: command.MaxUses,
            minimumOrderAmount: command.MinimumOrderAmount,
            maxDiscountAmount: command.MaxDiscountAmount,
            isActive: command.IsActive);

        if (!updateResult.IsSuccess)
            return Result<Guid>.Fail(updateResult.GetErrorOrThrow());

        return Result<Guid>.Ok(promoCode.Id);
    }
}
