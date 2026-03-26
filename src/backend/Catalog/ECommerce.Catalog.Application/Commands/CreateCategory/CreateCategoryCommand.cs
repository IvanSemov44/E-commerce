using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    Guid? ParentId
) : IRequest<Result<CategoryDto>>;
