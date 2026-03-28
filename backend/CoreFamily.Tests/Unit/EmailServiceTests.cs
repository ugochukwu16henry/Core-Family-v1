using CoreFamily.API.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreFamily.Tests.Unit;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _emailService = new EmailService(_mockLogger.Object);
    }

    [Fact]
    public async Task SendAchievementUnlockedAsync_WithValidData_LogsSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var userName = "John Doe";
        var achievementName = "First Step";
        var points = 10;

        // Act
        await _emailService.SendAchievementUnlockedAsync(email, userName, achievementName, points);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendCertificateIssuedAsync_WithValidData_LogsSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var userName = "Jane Doe";
        var programTitle = "Family Communication";
        var certificateCode = "CERT123ABC456";

        // Act
        await _emailService.SendCertificateIssuedAsync(email, userName, programTitle, certificateCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMilestoneReachedAsync_WithValidData_LogsSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var userName = "Bob Smith";
        var programsCompleted = 5;

        // Act
        await _emailService.SendMilestoneReachedAsync(email, userName, programsCompleted);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendStreakNotificationAsync_WithValidData_LogsSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var userName = "Alice Johnson";
        var streakDays = 7;

        // Act
        await _emailService.SendStreakNotificationAsync(email, userName, streakDays);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid@example.com")]
    public async Task SendAchievementUnlockedAsync_WithInvalidEmail_ThrowsOrHandlesGracefully(string invalidEmail)
    {
        // Arrange
        var userName = "Test User";
        var achievementName = "Achievement";
        var points = 10;

        // Act & Assert - Should not throw, should log the error
        // The service is designed to handle errors gracefully
        await _emailService.SendAchievementUnlockedAsync(invalidEmail, userName, achievementName, points);
    }
}
