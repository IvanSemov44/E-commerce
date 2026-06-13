using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Queries;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryDto>>;
