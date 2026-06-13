using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Queries;

public record GetCategoryBySlugQuery(string Slug) : IRequest<Result<CategoryDto>>;
