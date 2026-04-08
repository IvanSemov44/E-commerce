using Microsoft.AspNetCore.Mvc;
using ECommerce.SharedKernel.Results;

namespace ECommerce.API.Common.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/> by invoking
    /// <paramref name="onSuccess"/> on the data or <paramref name="onFailure"/> on the error.
    /// Eliminates the IsSuccess/GetDataOrThrow boilerplate in every controller action.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult> onSuccess,
        Func<DomainError, IActionResult> onFailure)
        => result is Result<T>.Success s ? onSuccess(s.Data) : onFailure(((Result<T>.Failure)result).Error);

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> (void operations) to an <see cref="IActionResult"/>
    /// by invoking <paramref name="onSuccess"/> or <paramref name="onFailure"/> on the error.
    /// </summary>
    public static IActionResult ToActionResult(
        this Result result,
        Func<IActionResult> onSuccess,
        Func<DomainError, IActionResult> onFailure)
        => result is Result.Success ? onSuccess() : onFailure(((Result.Failure)result).Error);
}


