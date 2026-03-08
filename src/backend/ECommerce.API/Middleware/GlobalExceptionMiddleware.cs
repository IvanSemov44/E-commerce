using System.Net;
using System.Text.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Middleware;

/// <summary>
/// Global exception middleware for handling all unhandled exceptions.
/// Catches unexpected exceptions from the entire pipeline and returns standardized ApiResponse.
///
/// NOTE: Business logic failures are handled via Result{T} pattern - exceptions are reserved for
/// unexpected infrastructure failures only (database conflicts, network issues, etc.).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger instance for exception logging.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to catch and handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Handles exceptions by mapping them to appropriate HTTP status codes and responses.
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        context.Response.ContentType = "application/json";

        var response = MapExceptionToResponse(exception);
        context.Response.StatusCode = response.StatusCode;

        return context.Response.WriteAsJsonAsync(response.ApiResponse);
    }

    /// <summary>
    /// Maps different exception types to appropriate HTTP status codes and error messages.
    /// Only handles unexpected infrastructure failures - business logic uses Result{T}.
    /// </summary>
    private static (int StatusCode, ApiResponse<object> ApiResponse) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            // Not Found (404) - Base exception type for infrastructure resource lookup failures
            NotFoundException => (StatusCodes.Status404NotFound,
                ApiResponse<object>.Failure(exception.Message, "NOT_FOUND")),

            // Unauthorized (401) - Base exception type for authentication failures
            UnauthorizedException => (StatusCodes.Status401Unauthorized,
                ApiResponse<object>.Failure(exception.Message, "UNAUTHORIZED")),

            // Bad Request (400) - Base exception type for malformed requests
            BadRequestException => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure(exception.Message, "BAD_REQUEST")),

            // Argument validation errors (400) - Framework exceptions
            ArgumentNullException argNullEx => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure($"Missing required parameter: {argNullEx.ParamName}", "MISSING_PARAMETER")),

            ArgumentException argEx => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure(argEx.Message, "INVALID_ARGUMENT")),

            // Conflict (409) - Base exception type for conflict scenarios
            ConflictException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure(exception.Message, "CONFLICT")),

            // Concurrency conflict - EF Core optimistic locking violations (409)
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure(
                    "The resource was modified by another user. Please refresh and try again.",
                    "CONCURRENCY_CONFLICT")),

            // Invalid operation - Framework exceptions (409)
            InvalidOperationException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure("The requested operation could not be completed due to a conflict.", "INVALID_OPERATION")),

            // Generic exception - Internal Server Error (500)
            // Note: exception.Message is logged but NOT exposed to client for security
            _ => (StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Failure(
                    "An internal server error occurred. Please try again later.",
                    "INTERNAL_SERVER_ERROR"))
        };
    }
}
