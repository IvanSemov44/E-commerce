using MediatR;
using System.Diagnostics;

namespace ECommerce.API.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMs}ms (threshold {ThresholdMs}ms). Request: {@Request}",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                SlowRequestThresholdMs,
                request);
        }

        return response;
    }
}
