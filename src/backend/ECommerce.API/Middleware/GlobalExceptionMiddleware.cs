using System.Net;
using System.Text.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Exceptions.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Middleware;

/// <summary>
/// Global exception middleware for handling all unhandled exceptions.
/// Catches exceptions from the entire pipeline and returns standardized ApiResponse.
/// Implements CodeMaze best practices for centralized exception handling.
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
    /// </summary>
    private (int StatusCode, ApiResponse<object> ApiResponse) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            // Not Found (404)
            NotFoundException => (StatusCodes.Status404NotFound,
                ApiResponse<object>.Failure(exception.Message, "NOT_FOUND")),

            // Unauthorized (401)
            UnauthorizedException => (StatusCodes.Status401Unauthorized,
                ApiResponse<object>.Failure(exception.Message, "UNAUTHORIZED")),

            // Bad Request (400)
            BadRequestException => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure(exception.Message, "BAD_REQUEST")),

            // Argument validation errors (400)
            ArgumentNullException argNullEx => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure($"Missing required parameter: {argNullEx.ParamName}", "MISSING_PARAMETER")),

            ArgumentException argEx => (StatusCodes.Status400BadRequest,
                ApiResponse<object>.Failure(argEx.Message, "INVALID_ARGUMENT")),

            // Invalid operation (409)
            InvalidOperationException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure("The requested operation could not be completed due to a conflict.", "INVALID_OPERATION")),

            // Conflict (409)
            ConflictException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure(exception.Message, "CONFLICT")),

            // Concurrency conflict - DbUpdateConcurrencyException (409)
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure(
                    "The resource was modified by another user. Please refresh and try again.",
                    "CONCURRENCY_CONFLICT")),

            // Generic exception - Internal Server Error (500)
            // Note: exception.Message is logged but NOT exposed to client for security
            _ => (StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Failure(
                    "An internal server error occurred. Please try again later.",
                    "INTERNAL_SERVER_ERROR"))
        };
    }
}
