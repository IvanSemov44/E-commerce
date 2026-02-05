using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.API.ActionFilters;

/// <summary>
/// Action filter for automatic model state validation.
/// Eliminates the need for manual try-catch validation blocks in controllers.
/// Returns standardized ApiResponse with validation errors.
/// </summary>
public class ValidationFilterAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Executes when the action is executing - validates the request before controller action runs.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var action = context.RouteData.Values["action"];
        var controller = context.RouteData.Values["controller"];

        // Check for null DTO parameter (supports Dto and Request types)
        var param = context.ActionArguments
            .SingleOrDefault(x => 
            {
                var typeName = x.Value?.GetType().Name ?? "";
                return typeName.EndsWith("Dto") || typeName.EndsWith("Request");
            }).Value;

        if (param is null)
        {
            var error = $"Object is null. Controller: {controller}, action: {action}";
            var errorResponse = ApiResponse<object>.Error(error, new List<string> { error });
            context.Result = new BadRequestObjectResult(errorResponse);
            return;
        }

        // Validate ModelState
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var errorResponse = ApiResponse<object>.Error("Validation failed", errors);
            context.Result = new UnprocessableEntityObjectResult(errorResponse);
        }
    }

}
