using MediatR;

namespace ECommerce.SharedKernel.Domain;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
