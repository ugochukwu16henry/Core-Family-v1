using System.Security.Claims;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorProgramsController : BaseApiController
{
    private readonly IProgramService _programs;

    public InstructorProgramsController(IProgramService programs) => _programs = programs;

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<InstructorProgramSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPrograms()
        => Ok(await _programs.GetMyProgramsAsync(GetCurrentUserId()));

    [HttpPost]
    [ProducesResponseType(typeof(InstructorProgramSummaryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProgram([FromBody] InstructorProgramUpsertDto dto)
    {
        var program = await _programs.CreateProgramAsync(GetCurrentUserId(), dto);
        return StatusCode(StatusCodes.Status201Created, program);
    }

    [HttpPut("{programId:guid}")]
    [ProducesResponseType(typeof(InstructorProgramSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProgram(Guid programId, [FromBody] InstructorProgramUpsertDto dto)
        => Ok(await _programs.UpdateProgramAsync(GetCurrentUserId(), programId, dto));

    [HttpPost("{programId:guid}/publish")]
    [ProducesResponseType(typeof(InstructorProgramSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishProgram(Guid programId)
        => Ok(await _programs.PublishProgramAsync(GetCurrentUserId(), programId));

    [HttpPost("{programId:guid}/lessons")]
    [ProducesResponseType(typeof(InstructorLessonSummaryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddLesson(Guid programId, [FromBody] InstructorLessonUpsertDto dto)
    {
        var lesson = await _programs.AddLessonAsync(GetCurrentUserId(), programId, dto);
        return StatusCode(StatusCodes.Status201Created, lesson);
    }

    [HttpPut("{programId:guid}/lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(InstructorLessonSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLesson(Guid programId, Guid lessonId, [FromBody] InstructorLessonUpsertDto dto)
        => Ok(await _programs.UpdateLessonAsync(GetCurrentUserId(), programId, lessonId, dto));

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}
