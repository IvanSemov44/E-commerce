using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.SharedKernel.Domain;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(CancellationToken cancellationToken = default);
}
