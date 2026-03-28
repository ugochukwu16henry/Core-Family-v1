using System.Security.Claims;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

public class CounselorsController : BaseApiController
{
    private readonly ICounselorService _counselors;

    public CounselorsController(ICounselorService counselors) => _counselors = counselors;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CounselorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] CounselorSearchDto search)
        => Ok(await _counselors.SearchAsync(search));

    [Authorize]
    [HttpPost("match")]
    [ProducesResponseType(typeof(IReadOnlyList<CounselorMatchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Match([FromBody] CounselorMatchRequestDto request)
        => Ok(await _counselors.GetMatchesAsync(GetCurrentUserId(), request));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CounselorSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var counselor = await _counselors.GetByIdAsync(id);
        return counselor is null ? NotFound() : Ok(counselor);
    }

    [Authorize(Roles = "Counselor")]
    [HttpPut("me")]
    [ProducesResponseType(typeof(CounselorSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertMe([FromBody] CounselorProfileUpsertDto dto)
        => Ok(await _counselors.UpsertMyProfileAsync(GetCurrentUserId(), dto));

    [Authorize(Roles = "Counselor")]
    [HttpGet("me/sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions()
        => Ok(await _counselors.GetCounselorSessionsAsync(GetCurrentUserId()));

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}