using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Domain.Entities;

public class Content : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Body { get; set; }
    public ContentType Type { get; set; }
    public ContentCategory Category { get; set; }
    public bool IsFree { get; set; } = false;
    public decimal Price { get; set; } = 0;
    public string? ThumbnailUrl { get; set; }
    public Guid CreatedById { get; set; }
    public bool IsPublished { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    // Navigation
    public User CreatedBy { get; set; } = null!;
    public VideoContent? Video { get; set; }
}

public class VideoContent : BaseEntity
{
    public Guid ContentId { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string? CdnUrl { get; set; }
    public string? HlsUrl { get; set; }
    public int DurationSeconds { get; set; }
    public string? CaptionsUrl { get; set; }
    public long FileSizeBytes { get; set; }

    // Navigation
    public Content Content { get; set; } = null!;
}

public class Program_ : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationWeeks { get; set; }
    public bool IsPublished { get; set; } = false;
    public Guid InstructorId { get; set; }
    public ContentCategory Category { get; set; }

    // Navigation
    public User Instructor { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

public class Lesson : BaseEntity
{
    public Guid ProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public Guid ContentId { get; set; }
    public bool IsRequired { get; set; } = true;

    // Navigation
    public Program_ Program { get; set; } = null!;
    public Content Content { get; set; } = null!;
}

public class Enrollment : BaseEntity
{
    public Guid ProgramId { get; set; }
    public Guid UserId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Program_ Program { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class ProgressEntry : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ContentId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? QuizScore { get; set; }
    public int SecondsWatched { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;
    public Content Content { get; set; } = null!;
}
