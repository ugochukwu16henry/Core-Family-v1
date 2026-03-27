using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Application.DTOs;

// ── Auth ─────────────────────────────────────────────────────────
public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    string Category
);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserSummaryDto User
);

public record RefreshTokenDto(string RefreshToken);

public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Token, string NewPassword);
public record VerifyEmailDto(string Token);

// ── User ─────────────────────────────────────────────────────────
public record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Category,
    string? AvatarUrl
);

public record UpdateProfileDto(
    string? FirstName,
    string? LastName,
    string? Bio,
    string? Country,
    string? City,
    string? PhoneNumber,
    string? PreferredLanguage,
    string? TimeZone
);
