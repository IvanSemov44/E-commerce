using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Catalog.Application.DTOs.Categories;

namespace ECommerce.Catalog.Application.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Slug = null,
    Guid? ParentId = null
) : IRequest<Result<CategoryDto>>, ITransactionalCommand;
