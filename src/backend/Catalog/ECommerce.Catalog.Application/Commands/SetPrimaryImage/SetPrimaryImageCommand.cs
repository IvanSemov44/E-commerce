using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands;

public record SetPrimaryImageCommand(
    Guid ProductId,
    Guid ImageId
) : IRequest<Result>, ITransactionalCommand;
