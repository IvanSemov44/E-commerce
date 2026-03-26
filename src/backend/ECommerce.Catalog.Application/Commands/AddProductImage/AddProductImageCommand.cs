using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.AddProductImage;

public record AddProductImageCommand(
    Guid ProductId,
    string Url,
    string? AltText
) : IRequest<Result<ProductDetailDto>>;
