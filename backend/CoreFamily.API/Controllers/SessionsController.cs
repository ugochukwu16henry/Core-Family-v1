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

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}