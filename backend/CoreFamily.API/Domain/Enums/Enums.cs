namespace CoreFamily.API.Domain.Enums;

public enum UserRole
{
    Client = 1,
    Instructor = 2,
    Counselor = 3,
    Admin = 4,
    Moderator = 5
}

public enum UserCategory
{
    Single = 1,
    MarriedMan = 2,
    MarriedWoman = 3,
    Parent = 4,
    Family = 5,
    Youth = 6
}

public enum ContentType
{
    Article = 1,
    Video = 2,
    Quiz = 3,
    Worksheet = 4,
    Webinar = 5
}

public enum ContentCategory
{
    Married = 1,
    Singles = 2,
    Parenting = 3,
    FamilyFinance = 4,
    ConflictResolution = 5,
    General = 6
}

public enum SessionStatus
{
    Pending = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum TransactionType
{
    LessonPurchase = 1,
    ProgramEnrollment = 2,
    CounselingSession = 3,
    PlatformFee = 4,
    Payout = 5,
    Refund = 6
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum VerificationStatus
{
    NotSubmitted = 1,
    Pending = 2,
    Verified = 3,
    Rejected = 4,
    Expired = 5
}
