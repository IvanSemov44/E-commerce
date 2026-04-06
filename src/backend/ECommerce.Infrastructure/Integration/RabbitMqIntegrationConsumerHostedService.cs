using System.Text.Json;
using ECommerce.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Integration;

public sealed class RabbitMqIntegrationConsumerHostedService(
    IServiceScopeFactory serviceScopeFactory,
    RabbitMqTransportOptions options,
    ILogger<RabbitMqIntegrationConsumerHostedService> logger) : BackgroundService
{
    private const string ExchangeName = "integration.events";
    private const string ExchangeType = "topic";

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> _queueEventTypeMap = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["integration.ProductProjectionUpdatedIntegrationEvent"] = typeof(ProductProjectionUpdatedIntegrationEvent),
        ["integration.ProductImageProjectionUpdatedIntegrationEvent"] = typeof(ProductImageProjectionUpdatedIntegrationEvent),
        ["integration.PromoCodeProjectionUpdatedIntegrationEvent"] = typeof(PromoCodeProjectionUpdatedIntegrationEvent),
        ["integration.AddressProjectionUpdatedIntegrationEvent"] = typeof(AddressProjectionUpdatedIntegrationEvent),
        ["integration.InventoryStockProjectionUpdatedIntegrationEvent"] = typeof(InventoryStockProjectionUpdatedIntegrationEvent),
        ["integration.OrderPlacedIntegrationEvent"] = typeof(OrderPlacedIntegrationEvent),
        ["integration.InventoryReservedIntegrationEvent"] = typeof(InventoryReservedIntegrationEvent),
        ["integration.InventoryReservationFailedIntegrationEvent"] = typeof(InventoryReservationFailedIntegrationEvent)
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPollingLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "RabbitMQ integration consumer loop failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunPollingLoopAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            VirtualHost = options.VirtualHost,
            UserName = options.Username,
            Password = options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var queueName in _queueEventTypeMap.Keys)
        {
            var routingKey = queueName.Replace("integration.", string.Empty, StringComparison.Ordinal);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: ExchangeName,
                routingKey: routingKey,
                cancellationToken: stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var mapping in _queueEventTypeMap)
            {
                var result = await channel.BasicGetAsync(mapping.Key, autoAck: false, cancellationToken: stoppingToken);
                if (result is null)
                    continue;

                try
                {
                    var eventType = mapping.Value;
                    var deserializedEvent = JsonSerializer.Deserialize(result.Body.Span, eventType, _serializerOptions);
                    if (deserializedEvent is not IntegrationEvent integrationEvent)
                    {
                        logger.LogWarning("Failed to deserialize integration event from queue {Queue}", mapping.Key);
                        await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        continue;
                    }

                    using var scope = serviceScopeFactory.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
                    await dispatcher.DispatchAsync(integrationEvent, stoppingToken);

                    await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed processing message from queue {Queue}. Message will be requeued.", mapping.Key);
                    await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }
    }
}
