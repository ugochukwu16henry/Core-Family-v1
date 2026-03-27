using System.Security.Claims;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

public class ProgramsController : BaseApiController
{
    private readonly IProgramService _programs;

    public ProgramsController(IProgramService programs) => _programs = programs;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProgramSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedPrograms([FromQuery] ProgramSearchDto search)
        => Ok(await _programs.GetPublishedProgramsAsync(search));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublishedProgramById(Guid id)
    {
        var program = await _programs.GetPublishedProgramByIdAsync(id);
        return program is null ? NotFound() : Ok(program);
    }

    [Authorize]
    [HttpPost("{id:guid}/enroll")]
    [ProducesResponseType(typeof(EnrollmentSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(Guid id)
    {
        var enrollment = await _programs.EnrollAsync(GetCurrentUserId(), id);
        return StatusCode(StatusCodes.Status201Created, enrollment);
    }

    [Authorize]
    [HttpGet("me/enrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollments()
        => Ok(await _programs.GetMyEnrollmentsAsync(GetCurrentUserId()));

    [Authorize]
    [HttpGet("{id:guid}/learn")]
    [ProducesResponseType(typeof(ProgramLearningDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLearningProgram(Guid id)
        => Ok(await _programs.GetLearningProgramAsync(GetCurrentUserId(), id));

    [Authorize]
    [HttpGet("{programId:guid}/lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonPlayerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLesson(Guid programId, Guid lessonId)
        => Ok(await _programs.GetLessonAsync(GetCurrentUserId(), programId, lessonId));

    [Authorize]
    [HttpPost("{programId:guid}/lessons/{lessonId:guid}/progress")]
    [ProducesResponseType(typeof(LessonPlayerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLessonProgress(Guid programId, Guid lessonId, [FromBody] UpdateLessonProgressDto dto)
        => Ok(await _programs.UpdateLessonProgressAsync(GetCurrentUserId(), programId, lessonId, dto));

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}
