using Polly;

namespace ECommerce.Infrastructure.Resilience;

/// <summary>
/// Defines standard resilience policies for the application.
/// Uses Polly 8.4.1 for retry and timeout patterns to handle transient failures gracefully.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a retry policy for HTTP operations.
    /// - Retries 3 times with exponential backoff (2s → 4s → 8s)
    /// - Handles transient failures:  timeouts, 408, 429, and 5xx errors
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode &&
                ((int)r.StatusCode >= 500 || (int)r.StatusCode == 408 || (int)r.StatusCode == 429))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    /// <summary>
    /// Creates a timeout policy for HTTP operations.
    /// - Times out after 30 seconds to prevent hanging requests
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Creates a combined policy with retry and timeout for HTTP operations.
    /// Applied in order: Timeout → Retry
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedHttpPolicy()
    {
        var timeoutPolicy = GetHttpTimeoutPolicy();
        var retryPolicy = GetHttpRetryPolicy();
        return Policy.WrapAsync(timeoutPolicy, retryPolicy);
    }

    /// <summary>
    /// Creates a retry policy for database operations.
    /// - Retries 3 times with exponential backoff (100ms → 200ms → 400ms)
    /// - Handles transient  failures: timeouts and IO exceptions
    /// </summary>
    public static IAsyncPolicy GetDatabaseRetryPolicy()
    {
        return Policy
            .Handle<InvalidOperationException>()
            .Or<TimeoutException>()
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
    }

    /// <summary>
    /// Creates a timeout policy for database operations.
    /// - Times out after 30 seconds to prevent hanging database calls
    /// </summary>
    public static IAsyncPolicy GetDatabaseTimeoutPolicy()
    {
        return Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Creates a combined policy with retry and timeout for database operations.
    /// Applied in order: Timeout → Retry
    /// </summary>
    public static IAsyncPolicy GetCombinedDatabasePolicy()
    {
        var timeoutPolicy = GetDatabaseTimeoutPolicy();
        var retryPolicy = GetDatabaseRetryPolicy();
        return Policy.WrapAsync(timeoutPolicy, retryPolicy);
    }
}
