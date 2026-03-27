using System.Security.Claims;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

[Authorize]
public class SessionsController : BaseApiController
{
    private readonly ICounselorService _counselors;

    public SessionsController(ICounselorService counselors) => _counselors = counselors;

    [HttpPost]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Book([FromBody] BookSessionDto dto)
    {
        var session = await _counselors.BookSessionAsync(GetCurrentUserId(), dto);
        return CreatedAtAction(nameof(GetMySessions), new { }, session);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions()
        => Ok(await _counselors.GetClientSessionsAsync(GetCurrentUserId()));

    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var session = await _counselors.GetSessionByIdAsync(GetCurrentUserId(), sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{sessionId:guid}/confirm")]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(Guid sessionId)
        => Ok(await _counselors.ConfirmSessionAsync(GetCurrentUserId(), sessionId));

    [HttpPost("{sessionId:guid}/cancel")]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid sessionId)
        => Ok(await _counselors.CancelSessionAsync(GetCurrentUserId(), sessionId));

    [HttpPost("{sessionId:guid}/reschedule")]
    [ProducesResponseType(typeof(SessionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reschedule(Guid sessionId, [FromBody] RescheduleSessionDto dto)
        => Ok(await _counselors.RescheduleSessionAsync(GetCurrentUserId(), sessionId, dto));

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}