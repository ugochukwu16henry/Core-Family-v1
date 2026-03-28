using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public interface IAchievementSeeder
{
    Task SeedAsync();
}

public class AchievementSeeder : IAchievementSeeder
{
    private readonly CoreFamilyDbContext _db;

    public AchievementSeeder(CoreFamilyDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        // Check if achievements already exist
        if (await _db.Achievements.AnyAsync())
        {
            return; // Already seeded
        }

        var achievements = new[]
        {
            // Program Completion Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "First Step",
                Description = "Complete your first program and begin your learning journey",
                IconUrl = "🎯",
                UnlockThreshold = 1,
                AchievementType = "ProgramCompletion",
                Points = 10,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Early Achiever",
                Description = "Complete 3 programs and show your dedication",
                IconUrl = "⭐",
                UnlockThreshold = 3,
                AchievementType = "ProgramCompletion",
                Points = 25,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Dedicated Learner",
                Description = "Complete 5 programs and master multiple topics",
                IconUrl = "🏆",
                UnlockThreshold = 5,
                AchievementType = "ProgramCompletion",
                Points = 50,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Knowledge Master",
                Description = "Complete 10 programs and become an expert learner",
                IconUrl = "👑",
                UnlockThreshold = 10,
                AchievementType = "ProgramCompletion",
                Points = 100,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Unstoppable",
                Description = "Complete 20 programs and achieve mastery level",
                IconUrl = "🚀",
                UnlockThreshold = 20,
                AchievementType = "ProgramCompletion",
                Points = 200,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Learning Streak Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Week Warrior",
                Description = "Maintain a 7-day learning streak",
                IconUrl = "🔥",
                UnlockThreshold = 7,
                AchievementType = "Streak",
                Points = 25,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Month Master",
                Description = "Maintain a 30-day learning streak",
                IconUrl = "💪",
                UnlockThreshold = 30,
                AchievementType = "Streak",
                Points = 75,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Legend Status",
                Description = "Maintain a 100-day learning streak",
                IconUrl = "⚡",
                UnlockThreshold = 100,
                AchievementType = "Streak",
                Points = 250,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Speed Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Quick Learner",
                Description = "Complete a program within a week",
                IconUrl = "⚙️",
                UnlockThreshold = 1,
                AchievementType = "Speed",
                Points = 15,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Community Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Reviewer",
                Description = "Leave your first course review",
                IconUrl = "✍️",
                UnlockThreshold = 1,
                AchievementType = "Community",
                Points = 10,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "5-Star Rater",
                Description = "Leave 10 reviews with 5-star ratings",
                IconUrl = "⭐⭐⭐⭐⭐",
                UnlockThreshold = 10,
                AchievementType = "Community",
                Points = 50,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.Achievements.AddRangeAsync(achievements);
        await _db.SaveChangesAsync();
    }
}
