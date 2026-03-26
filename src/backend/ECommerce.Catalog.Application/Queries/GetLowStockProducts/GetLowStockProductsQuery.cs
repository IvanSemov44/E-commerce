using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;
using System.Collections.Generic;

namespace ECommerce.Catalog.Application.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery(int Threshold = 10) : IRequest<Result<List<ProductDto>>>;
