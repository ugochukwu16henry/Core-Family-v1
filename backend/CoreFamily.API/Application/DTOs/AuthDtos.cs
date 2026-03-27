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

// ── Counselors & Sessions ───────────────────────────────────────
public record CounselorSummaryDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Bio,
    string? Country,
    string? City,
    string PreferredLanguage,
    string? Specialization,
    decimal HourlyRateUsd,
    string[] Languages,
    bool AcceptsNewClients,
    string LicenseStatus,
    decimal AverageRating,
    int ReviewCount
);

public record CounselorProfileUpsertDto(
    string? LicenseUrl,
    string? QualificationsUrl,
    DateTime? LicenseExpiryDate,
    string? Specialization,
    decimal HourlyRateUsd,
    string[] Languages,
    string? AvailabilityJson,
    bool AcceptsNewClients
);

public class CounselorSearchDto
{
    public string? Language { get; init; }
    public string? Specialization { get; init; }
    public string? Country { get; init; }
    public bool? AcceptsNewClients { get; init; }
}

public record BookSessionDto(
    Guid CounselorId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Notes
);

public record RescheduleSessionDto(
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Notes
);

public record SessionSummaryDto(
    Guid Id,
    Guid CounselorId,
    Guid ClientId,
    string CounselorName,
    string ClientName,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Status,
    decimal AmountPaid,
    decimal PlatformCommission,
    bool IsPaid,
    string PaymentStatus,
    string? Notes,
    string? MeetingUrl
);
