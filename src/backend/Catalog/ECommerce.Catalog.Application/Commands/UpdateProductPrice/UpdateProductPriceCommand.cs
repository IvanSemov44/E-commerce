using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.UpdateProductPrice;

public record UpdateProductPriceCommand(
    Guid Id,
    decimal Price,
    string Currency
) : IRequest<Result<ProductDetailDto>>, ITransactionalCommand;
