using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductUpdatedEvent(Guid ProductId, string Name, decimal Price) : DomainEventBase;
