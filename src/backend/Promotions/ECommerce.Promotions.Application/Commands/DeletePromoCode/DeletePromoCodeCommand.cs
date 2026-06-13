using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public record DeletePromoCodeCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;
