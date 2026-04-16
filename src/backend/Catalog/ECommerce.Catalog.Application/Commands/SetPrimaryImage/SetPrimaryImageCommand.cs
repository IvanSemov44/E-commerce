using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.SetPrimaryImage;

public record SetPrimaryImageCommand(
    Guid ProductId,
    Guid ImageId
) : IRequest<Result<Guid>>, ITransactionalCommand;
