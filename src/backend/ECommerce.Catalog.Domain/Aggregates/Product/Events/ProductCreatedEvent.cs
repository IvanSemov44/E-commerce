using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Product.Events;

public record ProductCreatedEvent(Guid ProductId, string Name, Guid CategoryId) : DomainEventBase;
