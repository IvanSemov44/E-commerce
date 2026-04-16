using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.UpdateProductPrice;

public record UpdateProductPriceCommand(
    Guid Id,
    decimal Price,
    string Currency
) : IRequest<Result<Guid>>, ITransactionalCommand;
