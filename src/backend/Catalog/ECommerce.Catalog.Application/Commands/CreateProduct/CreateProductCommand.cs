using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    decimal Price,
    Guid CategoryId,
    string? Slug = null,
    string? Sku = null,
    string Currency = "USD",
    int? StockQuantity = null,
    string? Description = null,
    decimal? CompareAtPrice = null
) : IRequest<Result<Guid>>, ITransactionalCommand;
