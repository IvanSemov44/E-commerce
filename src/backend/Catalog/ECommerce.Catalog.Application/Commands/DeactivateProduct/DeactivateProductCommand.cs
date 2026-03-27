using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeactivateProduct;

public record DeactivateProductCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;
