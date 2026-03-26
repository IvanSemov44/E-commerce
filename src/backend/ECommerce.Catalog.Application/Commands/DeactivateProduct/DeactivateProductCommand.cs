using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Commands.DeactivateProduct;

public record DeactivateProductCommand(Guid Id) : IRequest<Result<ProductDetailDto>>;
