using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Domain.Entities;

public class CounselorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string? LicenseUrl { get; set; }
    public string? QualificationsUrl { get; set; }
    public VerificationStatus LicenseStatus { get; set; } = VerificationStatus.NotSubmitted;
    public DateTime? LicenseVerifiedAt { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string? Specialization { get; set; }
    public decimal HourlyRateUsd { get; set; }
    public string Languages { get; set; } = "en"; // comma-separated
    public string? AvailabilityJson { get; set; } // JSON calendar
    public bool AcceptsNewClients { get; set; } = true;
    public decimal AnnualFeeLastPaidUsd { get; set; } = 0;
    public DateTime? AnnualFeeLastPaidAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}

public class Session : BaseEntity
{
    public Guid CounselorId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public SessionStatus Status { get; set; } = SessionStatus.Pending;
    public decimal AmountPaid { get; set; }
    public decimal PlatformCommission { get; set; }
    public string? MeetingUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsRecorded { get; set; } = false;
    public string? RecordingUrl { get; set; }

    // Navigation
    public CounselorProfile Counselor { get; set; } = null!;
    public User Client { get; set; } = null!;
    public Review? Review { get; set; }
}

public class Review : BaseEntity
{
    public Guid ReviewerId { get; set; }
    public Guid? CounselorId { get; set; }
    public Guid? InstructorId { get; set; }
    public Guid? SessionId { get; set; }
    public int Rating { get; set; } // 1–5
    public string? ReviewText { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public bool IsFlagged { get; set; } = false;

    // Navigation
    public User Reviewer { get; set; } = null!;
    public CounselorProfile? Counselor { get; set; }
    public Session? Session { get; set; }
}
