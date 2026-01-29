using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace ECommerce.API.Extensions;

/// <summary>
/// Extension methods for configuring global exception handling middleware.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    /// <summary>
    /// Configures global exception handler middleware to catch and process all exceptions.
    /// This keeps controllers clean and provides consistent error responses to clients.
    /// </summary>
    /// <param name="app">The WebApplication instance.</param>
    /// <param name="logger">Logger for recording errors.</param>
    public static void ConfigureExceptionHandler(this WebApplication app, ILogger logger)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.ContentType = "application/json";
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                if (contextFeature != null)
                {
                    // Map exception types to HTTP status codes
                    context.Response.StatusCode = contextFeature.Error switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        BadRequestException => StatusCodes.Status400BadRequest,
                        UnauthorizedException => StatusCodes.Status401Unauthorized,
                        ConflictException => StatusCodes.Status409Conflict,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    // Log the error with appropriate severity
                    if (context.Response.StatusCode == StatusCodes.Status500InternalServerError)
                    {
                        logger.LogError(contextFeature.Error, "Internal server error occurred: {Message}", contextFeature.Error.Message);
                    }
                    else
                    {
                        logger.LogWarning("Client error occurred: {StatusCode} - {Message}", context.Response.StatusCode, contextFeature.Error.Message);
                    }

                    // Return standardized error response
                    await context.Response.WriteAsync(new ErrorDetails
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = contextFeature.Error.Message
                    }.ToString());
                }
            });
        });
    }
}
