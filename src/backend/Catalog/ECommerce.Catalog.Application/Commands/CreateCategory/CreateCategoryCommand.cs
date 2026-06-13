using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands;

public record CreateCategoryCommand(
    string Name,
    string? Slug = null,
    Guid? ParentId = null
) : IRequest<Result<Guid>>, ITransactionalCommand;
