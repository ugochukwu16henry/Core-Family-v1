using System.Security.Claims;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        var user = await _users.GetByIdAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Update the currently authenticated user's profile.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _users.UpdateProfileAsync(userId, dto);
        return Ok(result);
    }

    /// <summary>Get a user by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}
