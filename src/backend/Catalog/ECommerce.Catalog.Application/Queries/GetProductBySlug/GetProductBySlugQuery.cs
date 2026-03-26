using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Products;

namespace ECommerce.Catalog.Application.Queries.GetProductBySlug;

public record GetProductBySlugQuery(string Slug) : IRequest<Result<ProductDetailDto>>;
