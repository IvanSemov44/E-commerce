using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Infrastructure.Integration;

public sealed class OrderingOutboxDispatcherOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<OrderingOutboxDispatcherOptions>
{
    private const string SectionName = "IntegrationMessaging:Outbox";

    public void Configure(OrderingOutboxDispatcherOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}
