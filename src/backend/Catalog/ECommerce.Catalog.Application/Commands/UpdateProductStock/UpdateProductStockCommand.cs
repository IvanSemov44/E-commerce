using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.UpdateProductStock;

public record UpdateProductStockCommand(
    Guid Id,
    int Quantity,
    string Reason
) : IRequest<Result>, ITransactionalCommand;
