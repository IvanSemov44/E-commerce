using System;
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Application.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result>;
