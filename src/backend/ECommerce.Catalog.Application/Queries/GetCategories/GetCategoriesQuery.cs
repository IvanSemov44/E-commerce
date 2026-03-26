using System.Collections.Generic;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Queries.GetCategories;

public record GetCategoriesQuery() : IRequest<Result<IEnumerable<CategoryDto>>>;
