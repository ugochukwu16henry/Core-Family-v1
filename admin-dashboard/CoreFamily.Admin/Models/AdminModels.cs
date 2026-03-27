namespace CoreFamily.Admin.Models;

public record AdminUserSummary(
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

public record AdminReviewSummary(
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

public record AdminTransactionSummary(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string Type,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    Guid? ReferenceId,
    string? ExternalTransactionId,
    DateTime CreatedAt,
    string? FailureReason
);

public record SetUserActiveStatusRequest(bool IsActive);
public record SetReviewFlagStatusRequest(bool IsFlagged);
public record RefundTransactionRequest(string Reason);
