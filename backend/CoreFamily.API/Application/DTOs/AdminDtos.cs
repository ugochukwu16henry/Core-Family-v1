namespace CoreFamily.API.Application.DTOs;

public record AdminUserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Category,
    bool IsActive,
    bool EmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record AdminReviewSummaryDto(
    Guid Id,
    Guid ReviewerId,
    string ReviewerName,
    Guid? CounselorId,
    string? CounselorName,
    Guid? SessionId,
    int Rating,
    string? ReviewText,
    bool IsAnonymous,
    bool IsFlagged,
    DateTime CreatedAt
);

public record SetUserActiveStatusDto(
    bool IsActive
);

public record SetReviewFlagStatusDto(
    bool IsFlagged
);
