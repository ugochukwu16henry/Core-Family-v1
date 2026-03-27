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
