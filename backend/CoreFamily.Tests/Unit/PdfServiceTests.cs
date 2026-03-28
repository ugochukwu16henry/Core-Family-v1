using CoreFamily.API.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreFamily.Tests.Unit;

public class PdfServiceTests
{
    private readonly Mock<ILogger<PdfService>> _mockLogger;
    private readonly PdfService _pdfService;

    public PdfServiceTests()
    {
        _mockLogger = new Mock<ILogger<PdfService>>();
        _pdfService = new PdfService(_mockLogger.Object);
    }

    [Fact]
    public async Task GenerateCertificatePdfAsync_WithValidData_ReturnsByteArray()
    {
        // Arrange
        var recipientName = "John Doe";
        var programTitle = "Family Communication 101";
        var certificateCode = "CERT20260328001";
        var completionDate = DateTime.UtcNow;

        // Act
        var result = await _pdfService.GenerateCertificatePdfAsync(
            recipientName,
            programTitle,
            certificateCode,
            completionDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.True(result.Length > 0, "PDF content should not be empty");
    }

    [Fact]
    public async Task GenerateCertificatePdfAsync_WithDifferentNames_ProducesDifferentContent()
    {
        // Arrange
        var name1 = "John Doe";
        var name2 = "Jane Smith";
        var programTitle = "Family Communication 101";
        var certificateCode1 = "CERT1";
        var certificateCode2 = "CERT2";
        var completionDate = DateTime.UtcNow;

        // Act
        var result1 = await _pdfService.GenerateCertificatePdfAsync(
            name1, programTitle, certificateCode1, completionDate);
        var result2 = await _pdfService.GenerateCertificatePdfAsync(
            name2, programTitle, certificateCode2, completionDate);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public async Task GenerateProgressReportPdfAsync_WithValidData_ReturnsByteArray()
    {
        // Arrange
        var userName = "John Doe";
        var completedPrograms = new List<(string ProgramTitle, DateTime CompletionDate)>
        {
            ("Program 1", DateTime.UtcNow.AddDays(-30)),
            ("Program 2", DateTime.UtcNow.AddDays(-15))
        };
        var unlockedAchievements = new List<(string AchievementName, int Points)>
        {
            ("First Step", 10),
            ("Early Achiever", 25)
        };
        var currentStreak = 7;

        // Act
        var result = await _pdfService.GenerateProgressReportPdfAsync(
            userName,
            completedPrograms,
            unlockedAchievements,
            currentStreak);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.True(result.Length > 0, "PDF content should not be empty");
    }

    [Fact]
    public async Task GenerateProgressReportPdfAsync_WithEmptyCollections_StillReturnsContent()
    {
        // Arrange
        var userName = "Jane Smith";
        var completedPrograms = new List<(string, DateTime)>();
        var unlockedAchievements = new List<(string, int)>();
        var currentStreak = 0;

        // Act
        var result = await _pdfService.GenerateProgressReportPdfAsync(
            userName,
            completedPrograms,
            unlockedAchievements,
            currentStreak);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.True(result.Length > 0, "PDF content should not be empty even with no data");
    }

    [Fact]
    public async Task GenerateCertificatePdfAsync_LogsInformationMessage()
    {
        // Arrange
        var recipientName = "Test User";
        var programTitle = "Test Program";
        var certificateCode = "TEST123";
        var completionDate = DateTime.UtcNow;

        // Act
        await _pdfService.GenerateCertificatePdfAsync(
            recipientName,
            programTitle,
            certificateCode,
            completionDate);

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
    public async Task GenerateProgressReportPdfAsync_LogsInformationMessage()
    {
        // Arrange
        var userName = "Test User";
        var completedPrograms = new List<(string, DateTime)> { ("Program", DateTime.UtcNow) };
        var unlockedAchievements = new List<(string, int)> { ("Achievement", 10) };

        // Act
        await _pdfService.GenerateProgressReportPdfAsync(
            userName,
            completedPrograms,
            unlockedAchievements,
            5);

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
}
