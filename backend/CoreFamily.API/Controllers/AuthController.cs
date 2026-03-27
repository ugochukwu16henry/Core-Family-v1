using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

public class AuthController : BaseApiController
{
    // POST api/v1/auth/register
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Registration endpoint — coming soon" });
    }

    // POST api/v1/auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Login endpoint — coming soon" });
    }

    // POST api/v1/auth/refresh
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Token refresh — coming soon" });
    }

    // POST api/v1/auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Logout — coming soon" });
    }

    // POST api/v1/auth/forgot-password
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Password reset initiated" });
    }

    // POST api/v1/auth/verify-email
    [HttpPost("verify-email")]
    public IActionResult VerifyEmail([FromQuery] string token)
    {
        // TODO: implement in Phase 1
        return Ok(new { message = "Email verification — coming soon" });
    }
}

// ── Request DTOs (move to DTOs folder in Phase 1) ────────────────
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,       // Client | Instructor | Counselor
    string Category    // Single | MarriedMan | MarriedWoman | Parent | Family | Youth
);

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
