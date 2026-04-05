using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Promotions.Application.Commands.DeletePromoCode;

public record DeletePromoCodeCommand(Guid Id) : IRequest<Result>;
