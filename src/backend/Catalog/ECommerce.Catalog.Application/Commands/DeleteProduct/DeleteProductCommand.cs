using System;
using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Catalog.Application.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Result>, ITransactionalCommand;
