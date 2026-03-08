namespace ECommerce.Application.Interfaces;

public enum IdempotencyStartStatus
{
    Acquired,
    Replay,
    InProgress
}

public sealed record IdempotencyStartResult<T>(IdempotencyStartStatus Status, T? CachedResponse = null) where T : class;

/// <summary>
/// Stores successful operation results for idempotent request replay.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically starts processing for a key.
    /// Returns replay data if a completed response already exists.
    /// </summary>
    Task<IdempotencyStartResult<T>> StartAsync<T>(string key, TimeSpan inProgressTtl, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Completes processing and stores a replayable successful response.
    /// </summary>
    Task CompleteAsync<T>(string key, T value, TimeSpan completedTtl, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Abandons processing for the key (used for failed/non-cacheable outcomes).
    /// </summary>
    Task AbandonAsync(string key, CancellationToken cancellationToken = default);
}
