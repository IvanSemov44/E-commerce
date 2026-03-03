using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.API.Controllers;

/// <summary>
/// Base controller providing consistent API response formatting and helper methods.
/// All controllers should inherit from this to ensure uniform response structure.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Returns a successful 200 OK response with data.
    /// </summary>
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string message = "Success") =>
        Ok(ApiResponse<T>.Ok(data, message));

    /// <summary>
    /// Returns a successful 201 Created response with data and location header.
    /// </summary>
    protected ActionResult<ApiResponse<T>> CreatedAtActionResponse<T>(
        string actionName,
        object? routeValues,
        T data,
        string message = "Resource created successfully") =>
        CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, message));

    /// <summary>
    /// Returns a successful 204 No Content response.
    /// Used when operation succeeds but returns no data (e.g., DELETE).
    /// </summary>
    protected IActionResult NoContentResponse() =>
        NoContent();

    /// <summary>
    /// Returns a successful 200 OK response without data wrapper (for simple pagination, etc.).
    /// </summary>
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string message, int statusCode) =>
        StatusCode(statusCode, ApiResponse<T>.Ok(data, message));

    /// <summary>
    /// Returns a 400 Bad Request with validation errors.
    /// </summary>
    protected ActionResult<ApiResponse<object>> BadRequestResponse(
        string message = "Validation failed",
        Dictionary<string, List<string>>? errors = null)
    {
        var validationErrors = errors?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );
        var response = ApiResponse<object>.Failure(message, "VALIDATION_FAILED", validationErrors);
        return BadRequest(response);
    }

    /// <summary>
    /// Returns a 404 Not Found response.
    /// </summary>
    protected ActionResult<ApiResponse<object>> NotFoundResponse(string message = "Resource not found", string code = "NOT_FOUND") =>
        NotFound(ApiResponse<object>.Failure(message, code));

    /// <summary>
    /// Returns a 409 Conflict response (typically for duplicate resources, violations, etc.).
    /// </summary>
    protected ActionResult<ApiResponse<object>> ConflictResponse(string message = "Resource conflict", string code = "CONFLICT") =>
        Conflict(ApiResponse<object>.Failure(message, code));

    /// <summary>
    /// Returns a 401 Unauthorized response.
    /// </summary>
    protected ActionResult<ApiResponse<object>> UnauthorizedResponse(string message = "Unauthorized", string code = "UNAUTHORIZED") =>
        Unauthorized(ApiResponse<object>.Failure(message, code));

    /// <summary>
    /// Returns a 403 Forbidden response (authenticated but no permission).
    /// </summary>
    protected ActionResult<ApiResponse<object>> ForbiddenResponse(string message = "Access denied", string code = "FORBIDDEN") =>
        StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Failure(message, code));

    /// <summary>
    /// Returns a 500 Internal Server Error response.
    /// </summary>
    protected ActionResult<ApiResponse<object>> InternalServerErrorResponse(
        string message = "An internal server error occurred", string code = "INTERNAL_SERVER_ERROR") =>
        StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Failure(message, code));

    /// <summary>
    /// Gets the correlation ID from the HTTP context for logging/tracing.
    /// </summary>
    protected string? GetCorrelationId() =>
        HttpContext.Items["CorrelationId"]?.ToString();
}
