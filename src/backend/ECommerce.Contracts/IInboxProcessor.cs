using ECommerce.Contracts;

namespace ECommerce.Contracts;

public interface IInboxProcessor
{
    Task ExecuteAsync<TEvent>(
        TEvent integrationEvent,
        Func<CancellationToken, Task> handleAsync,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent;
}
