using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Identity.Infrastructure.Integration;

public sealed class IdentityOutboxDispatcherOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<IdentityOutboxDispatcherOptions>
{
    public void Configure(IdentityOutboxDispatcherOptions options)
    {
        var section = configuration.GetSection("IntegrationMessaging:Outbox");

        options.BatchSize             = ParseOrDefault(section["BatchSize"],             options.BatchSize);
        options.MaxRetryAttempts      = ParseOrDefault(section["MaxRetryAttempts"],      options.MaxRetryAttempts);
        options.BaseRetryDelaySeconds = ParseOrDefault(section["BaseRetryDelaySeconds"], options.BaseRetryDelaySeconds);
        options.MaxRetryDelaySeconds  = ParseOrDefault(section["MaxRetryDelaySeconds"],  options.MaxRetryDelaySeconds);
    }

    private static int ParseOrDefault(string? value, int fallback)
        => int.TryParse(value, out var parsed) ? parsed : fallback;
}
