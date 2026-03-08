using System.Collections.Concurrent;
using ECommerce.Application.Interfaces;

namespace ECommerce.Application.Services;

/// <summary>
/// In-memory idempotency store for development/testing.
/// </summary>
public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private const string InProgressState = "in_progress";
    private const string CompletedState = "completed";

    private sealed record CacheEntry(string State, object? Value, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task<IdempotencyStartResult<T>> StartAsync<T>(string key, TimeSpan inProgressTtl, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_store.TryGetValue(key, out var entry))
        {
            _store[key] = new CacheEntry(InProgressState, null, DateTimeOffset.UtcNow.Add(inProgressTtl));
            return Task.FromResult(new IdempotencyStartResult<T>(IdempotencyStartStatus.Acquired));
        }

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _store.TryRemove(key, out _);
            _store[key] = new CacheEntry(InProgressState, null, DateTimeOffset.UtcNow.Add(inProgressTtl));
            return Task.FromResult(new IdempotencyStartResult<T>(IdempotencyStartStatus.Acquired));
        }

        if (entry.State == CompletedState && entry.Value is T cachedResponse)
            return Task.FromResult(new IdempotencyStartResult<T>(IdempotencyStartStatus.Replay, cachedResponse));

        return Task.FromResult(new IdempotencyStartResult<T>(IdempotencyStartStatus.InProgress));
    }

    public Task CompleteAsync<T>(string key, T value, TimeSpan completedTtl, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expiresAt = DateTimeOffset.UtcNow.Add(completedTtl);
        _store[key] = new CacheEntry(CompletedState, value, expiresAt);
        return Task.CompletedTask;
    }

    public Task AbandonAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
