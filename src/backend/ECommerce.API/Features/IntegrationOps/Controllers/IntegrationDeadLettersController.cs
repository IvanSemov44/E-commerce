using System.Collections.Frozen;
using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Common;
using ECommerce.Infrastructure.Integration;
using ECommerce.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.IntegrationOps.Controllers;

[ApiController]
[Route("api/integration/dead-letters")]
[Produces("application/json")]
[Tags("Integration")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class IntegrationDeadLettersController(IDeadLetterReplayService deadLetterReplayService) : ControllerBase
{
    private static readonly FrozenSet<string> _notFound = FrozenSet.Create("DEAD_LETTER_MESSAGE_NOT_FOUND");
    private static readonly FrozenSet<string> _conflict = FrozenSet.Create("DEAD_LETTER_ALREADY_REQUEUED");
    private static readonly FrozenSet<string> _unprocessable = FrozenSet.Create("INVALID_INTEGRATION_EVENT_PAYLOAD");

    private IActionResult MapError(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);
        if (_notFound.Contains(error.Code))      return NotFound(body);
        if (_conflict.Contains(error.Code))      return Conflict(body);
        if (_unprocessable.Contains(error.Code)) return UnprocessableEntity(body);
        return BadRequest(body);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DeadLetterPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDeadLetters(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeRequeued = false,
        CancellationToken cancellationToken = default)
    {
        var result = await deadLetterReplayService.GetDeadLettersAsync(page, pageSize, includeRequeued, cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<DeadLetterPageDto>.Ok(data, "Dead-letter messages retrieved successfully")),
            MapError);
    }

    [HttpPost("{id:guid}/requeue")]
    [ProducesResponseType(typeof(ApiResponse<DeadLetterMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequeueDeadLetter(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await deadLetterReplayService.RequeueAsync(id, cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<DeadLetterMessageDto>.Ok(data, "Dead-letter message requeued successfully")),
            MapError);
    }
}
