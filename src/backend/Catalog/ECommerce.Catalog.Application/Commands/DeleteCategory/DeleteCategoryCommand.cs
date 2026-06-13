using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;
