using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Integration;

public sealed class OrderFulfillmentSagaTimeoutHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OrderFulfillmentSagaOptions> options,
    ILogger<OrderFulfillmentSagaTimeoutHostedService> logger) : BackgroundService
{
    private readonly OrderFulfillmentSagaOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollSeconds = Math.Max(5, _options.TimeoutPollIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(pollSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var sagaService = scope.ServiceProvider.GetRequiredService<IOrderFulfillmentSagaService>();
                var timedOutCount = await sagaService.HandleTimeoutsAsync(DateTime.UtcNow, stoppingToken);

                if (timedOutCount > 0)
                {
                    logger.LogWarning("Compensated {Count} timed-out order fulfillment saga instances", timedOutCount);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Order fulfillment saga timeout iteration failed");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
