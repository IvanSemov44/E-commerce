using System;
using ECommerce.SharedKernel.Domain;

namespace ECommerce.Catalog.Domain.Aggregates.Category.Events;

public record CategoryCreatedEvent(Guid CategoryId, string Name) : DomainEventBase;
