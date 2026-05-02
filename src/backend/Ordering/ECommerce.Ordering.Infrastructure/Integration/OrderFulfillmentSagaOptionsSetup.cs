using ECommerce.Ordering.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Ordering.Infrastructure.Integration;

public sealed class OrderFulfillmentSagaOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<OrderFulfillmentSagaOptions>
{
    private const string SectionName = "IntegrationMessaging:OrderFulfillmentSaga";

    public void Configure(OrderFulfillmentSagaOptions options)
        => configuration.GetSection(SectionName).Bind(options);
}
