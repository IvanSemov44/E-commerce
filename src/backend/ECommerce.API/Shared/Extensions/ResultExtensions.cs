using Microsoft.AspNetCore.Mvc;
using ECommerce.Contracts.DTOs.Common;
using ECommerce.SharedKernel.Results;

namespace ECommerce.API.Shared.Extensions;

public static class ResultExtensions
{
    // Default overloads — error handling is automatic via ErrorType on DomainError.

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult> onSuccess)
        => result.ToActionResult(onSuccess, error => error.ToHttpResult());

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<IActionResult> onSuccess)
        => result.ToActionResult(_ => onSuccess(), error => error.ToHttpResult());

    public static IActionResult ToActionResult(
        this Result result,
        Func<IActionResult> onSuccess)
        => result.ToActionResult(onSuccess, error => error.ToHttpResult());

    // Async overload — for success handlers that need to await a second operation (e.g. re-query after update).

    public static Task<IActionResult> ToActionResultAsync<T>(
        this Result<T> result,
        Func<T, Task<IActionResult>> onSuccess)
        => result switch
        {
            Result<T>.Success s => onSuccess(s.Data),
            Result<T>.Failure f => Task.FromResult(f.Error.ToHttpResult()),
            _ => throw new InvalidOperationException($"Unknown Result subtype: {result.GetType().Name}")
        };

    // Explicit overloads — kept for actions that need custom failure handling (e.g. auth cookie flows).

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult> onSuccess,
        Func<DomainError, IActionResult> onFailure)
        => result switch
        {
            Result<T>.Success s => onSuccess(s.Data),
            Result<T>.Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException($"Unknown Result subtype: {result.GetType().Name}")
        };

    public static IActionResult ToActionResult(
        this Result result,
        Func<IActionResult> onSuccess,
        Func<DomainError, IActionResult> onFailure)
        => result switch
        {
            Result.Success   => onSuccess(),
            Result.Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException($"Unknown Result subtype: {result.GetType().Name}")
        };

    private static IActionResult ToHttpResult(this DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        return error.Type switch
        {
            ErrorType.NotFound     => new NotFoundObjectResult(body),
            ErrorType.Conflict     => new ConflictObjectResult(body),
            ErrorType.Validation   => new UnprocessableEntityObjectResult(body),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(body),
            ErrorType.Forbidden    => new ObjectResult(body) { StatusCode = StatusCodes.Status403Forbidden },
            _                      => new BadRequestObjectResult(body)
        };
    }
}
