using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string? Description = null,
    decimal? CompareAtPrice = null
) : IRequest<Result<ProductDetailDto>>, ITransactionalCommand;
