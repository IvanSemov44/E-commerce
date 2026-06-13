using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeactivatePromoCode;

public record DeactivatePromoCodeCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;
