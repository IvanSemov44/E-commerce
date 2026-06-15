using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.API.ActionFilters;

/// <summary>
/// Guards against null request bodies and invalid data annotations (ModelState).
/// Does NOT run FluentValidation rules — those execute in the MediatR pipeline via ValidationBehavior.
/// </summary>
public class ValidationFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var action = context.RouteData.Values["action"];
        var controller = context.RouteData.Values["controller"];

        // Check for null DTO parameter (supports Dto and Request types)
        var param = context.ActionArguments
            .SingleOrDefault(x =>
            {
                var typeName = x.Value?.GetType().Name ?? "";
                return typeName.EndsWith("Dto",     StringComparison.OrdinalIgnoreCase)
                    || typeName.EndsWith("Request", StringComparison.OrdinalIgnoreCase)
                    || typeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase);
            }).Value;

        if (param is null)
        {
            var error = $"Object is null. Controller: {controller}, action: {action}";
            var errorResponse = ApiResponse<object>.Failure(error, "NULL_PARAMETER");
            context.Result = new BadRequestObjectResult(errorResponse);
            return;
        }

        // Validate ModelState
        if (!context.ModelState.IsValid)
        {
            var validationErrors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var errorResponse = ApiResponse<object>.Failure("Validation failed", "VALIDATION_FAILED", validationErrors);
            context.Result = new UnprocessableEntityObjectResult(errorResponse);
        }
    }
}
