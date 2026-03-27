using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public UserProfile? Profile { get; set; }
    public ICollection<UserRole_> Roles { get; set; } = [];
}

public class UserRole_ : BaseEntity
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public User User { get; set; } = null!;
}

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string? PhoneNumber { get; set; }
    public UserCategory Category { get; set; } = UserCategory.Single;
    public string? TimeZone { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
