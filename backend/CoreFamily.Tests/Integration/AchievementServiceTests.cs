using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using CoreFamily.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreFamily.Tests.Integration;

public class AchievementServiceTests : IAsyncLifetime
{
    private readonly CoreFamilyDbContext _context;
    private readonly ProgramService _programService;
    private readonly Guid _testUserId;

    public AchievementServiceTests()
    {
        var options = new DbContextOptionsBuilder<CoreFamilyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoreFamilyDbContext(options);
        var mockPaymentService = new Mock<IPaymentService>();
        var mockEmailService = new Mock<IEmailService>();
        var mockPdfService = new Mock<IPdfService>();
        var mockLogger = new Mock<ILogger<ProgramService>>();

        _programService = new ProgramService(
            _context,
            mockPaymentService.Object,
            mockEmailService.Object,
            mockPdfService.Object);

        _testUserId = Guid.NewGuid();
    }

    public async Task InitializeAsync()
    {
        await SeedTestData();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            PasswordHash = "hashed",
            IsActive = true
        };

        _context.Users.Add(user);

        // Create test achievements
        var achievements = new[]
        {
            new Achievement
            {
                Name = "First Step",
                Description = "Complete your first program",
                UnlockThreshold = 1,
                    AchievementType = "ProgramCompletion",
                Points = 10,
                IconUrl = "https://example.com/first-step.png"
            },
            new Achievement
            {
                Name = "Early Achiever",
                Description = "Complete 3 programs",
                UnlockThreshold = 3,
                    AchievementType = "ProgramCompletion",
                Points = 25,
                IconUrl = "https://example.com/early-achiever.png"
            },
            new Achievement
            {
                Name = "Week Warrior",
                Description = "Maintain a 7-day learning streak",
                UnlockThreshold = 7,
                    AchievementType = "Streak",
                Points = 50,
                IconUrl = "https://example.com/week-warrior.png"
            }
        };

        _context.Achievements.AddRange(achievements);

        // Create a learning streak for the user
        var streak = new LearningStreak
        {
            UserId = _testUserId,
            CurrentStreak = 0,
            LongestStreak = 0,
            StreakStartDate = DateTime.UtcNow
        };

        _context.LearningStreaks.Add(streak);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMyAchievementsAsync_WithNoUnlockedAchievements_ReturnsLockedAchievements()
    {
        // Act
        var result = await _programService.GetMyAchievementsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, achievement =>
        {
            Assert.False(achievement.IsUnlocked);
        });
    }

    [Fact]
    public async Task GetMyAchievementsAsync_WithUnlockedAchievement_ReturnsIncludingUnlocked()
    {
        // Arrange
        var achievement = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Name == "First Step");
        Assert.NotNull(achievement);

        var userAchievement = new UserAchievement
        {
            UserId = _testUserId,
            AchievementId = achievement.Id,
            UnlockedAt = DateTime.UtcNow
        };

        _context.UserAchievements.Add(userAchievement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _programService.GetMyAchievementsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var unlockedAchievement = result.FirstOrDefault(a => a.Name == "First Step");
        Assert.NotNull(unlockedAchievement);
        Assert.True(unlockedAchievement.IsUnlocked);
    }

    [Fact]
    public async Task GetMyStreakAsync_WhenNoStreakExists_CreatesNewStreak()
    {
        // Arrange
        var newUserId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            Email = "newuser@example.com",
            PasswordHash = "hashed",
            IsActive = true
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _programService.GetMyStreakAsync(newUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newUserId, result.UserId);
        Assert.Equal(0, result.CurrentStreak);
        Assert.Equal(0, result.LongestStreak);
    }

    [Fact]
    public async Task GetMyStreakAsync_WithExistingStreak_ReturnsStreakData()
    {
        // Act
        var result = await _programService.GetMyStreakAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(0, result.CurrentStreak); // Seeded with 0
        Assert.Equal(0, result.LongestStreak); // Seeded with 0
    }

    [Fact]
    public async Task GetMyStreakAsync_WithLongStreak_ReturnsCorrectDays()
    {
        // Arrange
        var streak = await _context.LearningStreaks
            .FirstOrDefaultAsync(s => s.UserId == _testUserId);
        Assert.NotNull(streak);

        streak.CurrentStreak = 15;
        streak.LongestStreak = 30;
        streak.LastActivityDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act
        var result = await _programService.GetMyStreakAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.CurrentStreak);
        Assert.Equal(30, result.LongestStreak);
    }

    [Fact]
    public async Task GetMyAchievementsAsync_AchievementsAreOrderedByUnlockDate()
    {
        // Arrange
        var achievement1 = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Name == "First Step");
        var achievement2 = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Name == "Early Achiever");

        Assert.NotNull(achievement1);
        Assert.NotNull(achievement2);

        _context.UserAchievements.AddRange(
            new UserAchievement
            {
                UserId = _testUserId,
                AchievementId = achievement1.Id,
                UnlockedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserAchievement
            {
                UserId = _testUserId,
                AchievementId = achievement2.Id,
                UnlockedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _programService.GetMyAchievementsAsync(_testUserId);

        // Assert
        var unlockedAchievements = result.Where(a => a.IsUnlocked).ToList();
        Assert.NotEmpty(unlockedAchievements);
        // Most recent should be first
        for (int i = 0; i < unlockedAchievements.Count - 1; i++)
        {
            Assert.True(unlockedAchievements[i].UnlockedAt >= unlockedAchievements[i + 1].UnlockedAt);
        }
    }

    [Fact]
    public async Task GetProgressSummaryAsync_WithNoEnrollments_ReturnsZeroStats()
    {
        // Act
        var result = await _programService.GetProgressSummaryAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalEnrollments);
        Assert.Equal(0, result.CompletedPrograms);
        Assert.Equal(0, result.InProgressPrograms);
        Assert.Equal(0, result.CompletionPercentage);
    }
}
