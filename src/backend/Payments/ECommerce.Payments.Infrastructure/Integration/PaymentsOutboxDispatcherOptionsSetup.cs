using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Payments.Infrastructure.Integration;

public sealed class PaymentsOutboxDispatcherOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<PaymentsOutboxDispatcherOptions>
{
    private const string SectionName = "IntegrationMessaging:Outbox";

    public void Configure(PaymentsOutboxDispatcherOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}
