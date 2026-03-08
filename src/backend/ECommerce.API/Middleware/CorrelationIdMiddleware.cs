using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ECommerce.API.Middleware;

/// <summary>
/// Middleware that enables correlation ID tracking for distributed tracing.
/// Extracts correlation ID from request headers, Activity.Current, or generates a new one.
/// Validates all inbound correlation IDs and enriches logs for tracing across services.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CorrelationIdLogProperty = "CorrelationId";
    private const int MaxCorrelationIdLength = 255;

    // Valid correlation ID: UUID or alphanumeric-hyphen-underscore (no spaces, special chars)
    private static readonly Regex ValidCorrelationIdPattern = new(@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Extracts or generates a validated correlation ID and makes it available throughout the request pipeline.
    /// Priority: 1) Inbound header (if valid), 2) Activity.Current?.TraceId (W3C trace context), 3) New GUID
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrGenerateCorrelationId(context, _logger);

        // Store in HttpContext for access throughout the request
        context.Items[CorrelationIdLogProperty] = correlationId;

        // Set HttpContext.TraceIdentifier to align with correlation ID
        // This ensures .NET diagnostic tools and exception handling see the same ID
        context.TraceIdentifier = correlationId;

        // Set response header using bracket notation (idempotent, no duplicates)
        // Bracket notation replaces any existing value; Add() can throw or create duplicates in some scenarios
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Enrich all Serilog logs with the correlation ID (automatic in LogContext)
        using (LogContext.PushProperty(CorrelationIdLogProperty, correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Gets a valid correlation ID from the request, or generates a new one.
    /// Priority: 1) Valid inbound header, 2) Activity.Current?.TraceId (W3C), 3) New GUID
    /// </summary>
    private static string GetOrGenerateCorrelationId(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // 1. Try to get from inbound header (if valid)
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdHeader))
        {
            var inboundId = correlationIdHeader.FirstOrDefault()?.Trim() ?? string.Empty;
            if (IsValidCorrelationId(inboundId))
            {
                return inboundId;
            }

            logger.LogWarning(
                "Rejected invalid correlation ID from request header {CorrelationIdHeader}; generating a new value.",
                CorrelationIdHeader);
        }

        // 2. Prefer W3C trace context (Activity.Current) if available
        // This aligns with distributed tracing standards and other services
        var traceId = Activity.Current?.TraceId;
        if (traceId.HasValue && traceId != default)
        {
            return traceId.Value.ToString();
        }

        // 3. Generate new GUID as fallback
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Validates correlation ID: must be non-empty, max 255 chars, alphanumeric/hyphen/underscore only.
    /// Prevents header injection and ensures safe logging.
    /// </summary>
    private static bool IsValidCorrelationId(string correlationId)
    {
        // Must not be empty or whitespace
        if (string.IsNullOrWhiteSpace(correlationId))
            return false;

        // Must not exceed reasonable length
        if (correlationId.Length > MaxCorrelationIdLength)
            return false;

        // Must match safe pattern: UUID or alphanumeric-hyphen-underscore
        // Rejects: special chars, spaces, control chars, multi-line sequences
        return ValidCorrelationIdPattern.IsMatch(correlationId);
    }
}
