using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.ActivateProduct;

public record ActivateProductCommand(Guid Id) : IRequest<Result<ProductDetailDto>>;
