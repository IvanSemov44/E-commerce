using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;

namespace ECommerce.Promotions.Application.Commands.CreatePromoCode;

public sealed class CreatePromoCodeCommandHandler(IPromoCodeRepository repository)
    : IRequestHandler<CreatePromoCodeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(command.Code.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null)
            return Result<Guid>.Fail(PromotionsErrors.DuplicateCode);

        var createResult = PromoCode.Create(
            command.Code,
            command.DiscountType,
            command.DiscountValue,
            command.ValidFrom,
            command.ValidUntil,
            command.MaxUses,
            command.MinimumOrderAmount,
            command.MaxDiscountAmount);

        if (!createResult.IsSuccess)
            return Result<Guid>.Fail(createResult.GetErrorOrThrow());

        var promoCode = createResult.GetDataOrThrow();
        await repository.AddAsync(promoCode, cancellationToken);

        return Result<Guid>.Ok(promoCode.Id);
    }
}
