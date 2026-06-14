using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description = null,
    Guid? CategoryId = null
) : IRequest<Result<ProductDetailDto>>, ITransactionalCommand;
