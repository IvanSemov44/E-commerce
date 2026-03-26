using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.SetPrimaryImage;

public record SetPrimaryImageCommand(
    Guid ProductId,
    Guid ImageId
) : IRequest<Result<ProductDetailDto>>;
