using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    Guid? ParentId
) : IRequest<Result<Guid>>, ITransactionalCommand;
