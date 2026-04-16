using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description = null,
    /// <summary>
    /// New category for the product. Null means keep the existing category unchanged.
    /// </summary>
    Guid? CategoryId = null
) : IRequest<Result<Guid>>, ITransactionalCommand;
