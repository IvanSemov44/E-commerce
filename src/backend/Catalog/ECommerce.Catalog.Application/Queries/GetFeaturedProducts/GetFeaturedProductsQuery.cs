using System;
using System.Collections.Generic;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Queries.GetFeaturedProducts;

public record GetFeaturedProductsQuery(int Limit = 10) : IRequest<Result<List<ProductDto>>>;
