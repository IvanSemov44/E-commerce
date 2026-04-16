using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.AddProductImage;

public record AddProductImageCommand(
    Guid ProductId,
    string Url,
    string? AltText
) : IRequest<Result<Guid>>, ITransactionalCommand;
