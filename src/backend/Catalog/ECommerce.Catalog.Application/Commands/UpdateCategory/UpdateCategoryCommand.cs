using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    Guid? ParentId
) : IRequest<Result<CategoryDto>>;
