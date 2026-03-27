using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin,Moderator")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
        => Ok(await _admin.GetUsersAsync());

    [HttpPost("users/{userId:guid}/status")]
    [ProducesResponseType(typeof(AdminUserSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetUserStatus(Guid userId, [FromBody] SetUserActiveStatusDto dto)
        => Ok(await _admin.SetUserActiveStatusAsync(userId, dto.IsActive));

    [HttpGet("reviews/flagged")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminReviewSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFlaggedReviews()
        => Ok(await _admin.GetFlaggedReviewsAsync());

    [HttpPost("reviews/{reviewId:guid}/flag")]
    [ProducesResponseType(typeof(AdminReviewSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetReviewFlag(Guid reviewId, [FromBody] SetReviewFlagStatusDto dto)
        => Ok(await _admin.SetReviewFlagStatusAsync(reviewId, dto.IsFlagged));
}
