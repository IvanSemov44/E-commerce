using System.Text.Json;
using ECommerce.Contracts;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.Integration;

public sealed class RabbitMqIntegrationEventBus(
    RabbitMqTransportOptions options,
    JsonSerializerOptions? serializerOptions = null) : IIntegrationEventBus
{
    private const string ExchangeName = "integration.events";
    private const string ExchangeType = "topic";

    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            VirtualHost = options.VirtualHost,
            UserName = options.Username,
            Password = options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var eventType = integrationEvent.GetType();
        var routingKey = eventType.Name;
        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, _serializerOptions);

        var properties = new BasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = eventType.AssemblyQualifiedName;
        properties.MessageId = integrationEvent.IdempotencyKey.ToString();

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
