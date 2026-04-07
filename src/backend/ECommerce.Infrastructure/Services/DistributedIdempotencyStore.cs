using System.Text.Json;
using System.Collections.Concurrent;
using ECommerce.SharedKernel.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Distributed idempotency store backed by IDistributedCache (Redis in production, memory fallback otherwise).
/// </summary>
public class DistributedIdempotencyStore : IIdempotencyStore
{
    private const string InProgressState = "in_progress";
    private const string CompletedState = "completed";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LocalKeyLocks = new();

    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _redisMultiplexer;
    private readonly ILogger<DistributedIdempotencyStore> _logger;

    private sealed record IdempotencyEnvelope(string State, string? Payload);

    public DistributedIdempotencyStore(
        IDistributedCache distributedCache,
        IEnumerable<IConnectionMultiplexer> redisMultiplexers,
        ILogger<DistributedIdempotencyStore> logger)
    {
        _distributedCache = distributedCache;
        _redisMultiplexer = redisMultiplexers.FirstOrDefault();
        _logger = logger;
    }

    public async Task<IdempotencyStartResult<T>> StartAsync<T>(string key, TimeSpan inProgressTtl, CancellationToken cancellationToken = default) where T : class
    {
        if (_redisMultiplexer is { IsConnected: true })
        {
            var redisResult = await TryStartWithRedisAsync<T>(key, inProgressTtl);
            if (redisResult != null)
                return redisResult;
        }

        var keyLock = LocalKeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            var serializedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            var envelope = DeserializeEnvelope(serializedValue, key);

            if (envelope == null)
            {
                await SetEnvelopeAsync(key, new IdempotencyEnvelope(InProgressState, null), inProgressTtl, cancellationToken);
                _logger.LogInformation("Idempotency reservation acquired for key {Key}", key);
                return new IdempotencyStartResult<T>(IdempotencyStartStatus.Acquired);
            }

            if (envelope.State == CompletedState && !string.IsNullOrWhiteSpace(envelope.Payload))
            {
                var cachedResponse = JsonSerializer.Deserialize<T>(envelope.Payload);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Idempotency replay hit for key {Key}", key);
                    return new IdempotencyStartResult<T>(IdempotencyStartStatus.Replay, cachedResponse);
                }
            }

            _logger.LogInformation("Idempotency key {Key} is already in progress", key);
            return new IdempotencyStartResult<T>(IdempotencyStartStatus.InProgress);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Idempotency cache unavailable while starting key {Key}. Proceeding without dedup lock.", key);
            return new IdempotencyStartResult<T>(IdempotencyStartStatus.Acquired);
        }
        finally
        {
            keyLock.Release();
        }
    }

    public async Task CompleteAsync<T>(string key, T value, TimeSpan completedTtl, CancellationToken cancellationToken = default) where T : class
    {
        var envelope = new IdempotencyEnvelope(CompletedState, JsonSerializer.Serialize(value));

        if (_redisMultiplexer is { IsConnected: true })
        {
            try
            {
                var database = _redisMultiplexer.GetDatabase();
                await database.StringSetAsync(key, JsonSerializer.Serialize(envelope), completedTtl);
                _logger.LogInformation("Idempotency completion stored (Redis) for key {Key}, TTL {TtlMinutes} minutes", key, completedTtl.TotalMinutes);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Redis unavailable while completing idempotency key {Key}. Falling back to distributed cache.", key);
            }
        }

        try
        {
            await SetEnvelopeAsync(key, envelope, completedTtl, cancellationToken);
            _logger.LogInformation("Idempotency completion stored for key {Key}, TTL {TtlMinutes} minutes", key, completedTtl.TotalMinutes);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Idempotency cache unavailable while completing key {Key}. Continuing without persistence.", key);
        }
    }

    public async Task AbandonAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_redisMultiplexer is { IsConnected: true })
        {
            try
            {
                var database = _redisMultiplexer.GetDatabase();
                await database.KeyDeleteAsync(key);
                _logger.LogDebug("Idempotency key abandoned (Redis) for key {Key}", key);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Redis unavailable while abandoning idempotency key {Key}. Falling back to distributed cache.", key);
            }
        }

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Idempotency key abandoned for key {Key}", key);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Idempotency cache unavailable while abandoning key {Key}.", key);
        }
    }

    private async Task<IdempotencyStartResult<T>?> TryStartWithRedisAsync<T>(string key, TimeSpan inProgressTtl) where T : class
    {
        if (_redisMultiplexer == null)
            return null;

        try
        {
            var database = _redisMultiplexer.GetDatabase();
            var inProgressEnvelope = JsonSerializer.Serialize(new IdempotencyEnvelope(InProgressState, null));

            var reserved = await database.StringSetAsync(key, inProgressEnvelope, inProgressTtl, When.NotExists);
            if (reserved)
            {
                _logger.LogInformation("Idempotency reservation acquired (Redis) for key {Key}", key);
                return new IdempotencyStartResult<T>(IdempotencyStartStatus.Acquired);
            }

            var existingValue = await database.StringGetAsync(key);
            var envelope = DeserializeEnvelope(existingValue.HasValue ? existingValue.ToString() : null, key);

            if (envelope == null)
                return new IdempotencyStartResult<T>(IdempotencyStartStatus.InProgress);

            if (envelope.State == CompletedState && !string.IsNullOrWhiteSpace(envelope.Payload))
            {
                var cachedResponse = JsonSerializer.Deserialize<T>(envelope.Payload);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Idempotency replay hit (Redis) for key {Key}", key);
                    return new IdempotencyStartResult<T>(IdempotencyStartStatus.Replay, cachedResponse);
                }
            }

            _logger.LogInformation("Idempotency key {Key} is already in progress (Redis)", key);
            return new IdempotencyStartResult<T>(IdempotencyStartStatus.InProgress);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Redis unavailable while starting idempotency key {Key}. Falling back to distributed cache.", key);
            return null;
        }
    }

    private async Task SetEnvelopeAsync(string key, IdempotencyEnvelope envelope, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(envelope), options, cancellationToken);
    }

    private IdempotencyEnvelope? DeserializeEnvelope(string? serializedValue, string key)
    {
        if (string.IsNullOrWhiteSpace(serializedValue))
            return null;

        try
        {
            return JsonSerializer.Deserialize<IdempotencyEnvelope>(serializedValue);
        }
        catch (JsonException jsonException)
        {
            _logger.LogWarning(jsonException, "Failed to deserialize idempotency envelope for key {Key}", key);
            return null;
        }
    }
}

