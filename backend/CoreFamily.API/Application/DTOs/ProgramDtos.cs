using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Application.DTOs;

public class ProgramSearchDto
{
    public ContentCategory? Category { get; init; }
    public string? Query { get; init; }
}

public record ProgramSummaryDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    int DurationWeeks,
    ContentCategory Category,
    string InstructorName,
    int LessonCount
);

public record LessonSummaryDto(
    Guid Id,
    string Title,
    int OrderIndex,
    bool IsRequired,
    string? ContentType,
    bool IsFree,
    decimal Price
);

public record ProgramDetailDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    int DurationWeeks,
    ContentCategory Category,
    Guid InstructorId,
    string InstructorName,
    IReadOnlyList<LessonSummaryDto> Lessons
);

public record EnrollmentSummaryDto(
    Guid EnrollmentId,
    Guid ProgramId,
    string ProgramTitle,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    int TotalLessons,
    int CompletedLessons
);

public record LessonPlayerDto(
    Guid LessonId,
    Guid ProgramId,
    Guid ContentId,
    string Title,
    int OrderIndex,
    bool IsRequired,
    string? ContentTitle,
    string? ContentDescription,
    string? ContentBody,
    string ContentType,
    bool IsFree,
    decimal Price,
    int SecondsWatched,
    DateTime? CompletedAt
);

public record ProgramLearningDto(
    Guid ProgramId,
    string ProgramTitle,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    int TotalLessons,
    int CompletedLessons,
    IReadOnlyList<LessonPlayerDto> Lessons
);

public record UpdateLessonProgressDto(
    int SecondsWatched,
    bool MarkCompleted
);

/// <summary>
/// Progress summary for a single user across all their enrollments
/// </summary>
public record ProgressSummaryDto(
    int TotalEnrollments,
    int CompletedPrograms,
    int InProgressPrograms,
    decimal CompletionPercentage,
    int TotalLessonsCompleted,
    int TotalLessonsEnrolled,
    DateTime? MostRecentCompletionDate,
    IReadOnlyList<EnrollmentSummaryDto> Enrollments
);

/// <summary>
/// Milestone and badge information
/// </summary>
public record MilestoneDto(
    Guid Id,
    string Name,
    string Description,
    int CompletionThreshold, // e.g., 1 for first completion, 5 for 5 programs
    bool IsUnlocked,
    DateTime? UnlockedAt
);

/// <summary>
/// Certificate information returned by API
/// </summary>
public record CertificateDto(
    Guid Id,
    Guid UserId,
    Guid? ProgramId,
    string CertificateCode,
    string PdfUrl,
    DateTime IssuedAt,
    string? ProgramTitle
);
