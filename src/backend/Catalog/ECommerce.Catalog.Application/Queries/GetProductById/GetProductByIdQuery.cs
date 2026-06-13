using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailDto>>;
