using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Application.DTOs;

public record InstructorProgramUpsertDto(
    string Title,
    string Description,
    decimal Price,
    int DurationWeeks,
    ContentCategory Category
);

public record InstructorLessonUpsertDto(
    string Title,
    int OrderIndex,
    bool IsRequired,
    string ContentTitle,
    string? ContentDescription,
    string? ContentBody,
    ContentType ContentType,
    bool IsFree,
    decimal Price,
    string? ThumbnailUrl = null
);

public record InstructorProgramSummaryDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    int DurationWeeks,
    ContentCategory Category,
    bool IsPublished,
    int LessonCount,
    DateTime UpdatedAt
);

public record InstructorLessonSummaryDto(
    Guid LessonId,
    Guid ProgramId,
    Guid ContentId,
    string Title,
    int OrderIndex,
    bool IsRequired,
    ContentType ContentType,
    bool IsFree,
    decimal Price,
    bool IsPublished
);
