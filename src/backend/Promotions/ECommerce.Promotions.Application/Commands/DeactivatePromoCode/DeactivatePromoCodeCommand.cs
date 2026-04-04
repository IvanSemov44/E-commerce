using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public record DeactivatePromoCodeCommand(Guid Id) : IRequest<Result<Unit>>;