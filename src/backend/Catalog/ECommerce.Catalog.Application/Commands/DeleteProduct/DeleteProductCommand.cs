using System;
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Application.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<Result>;
