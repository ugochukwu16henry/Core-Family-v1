namespace CoreFamily.API.Application.Interfaces;

/// <summary>
/// Interface for PDF certificate generation
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generate a certificate PDF for a completed program
    /// </summary>
    Task<byte[]> GenerateCertificatePdfAsync(
        string recipientName,
        string programTitle,
        string certificateCode,
        DateTime completionDate,
        string instructorName = "Core Family");

    /// <summary>
    /// Generate a progress report PDF for a user
    /// </summary>
    Task<byte[]> GenerateProgressReportPdfAsync(
        string userName,
        List<(string ProgramTitle, DateTime CompletionDate)> completedPrograms,
        List<(string AchievementName, int Points)> unlockedAchievements,
        int currentStreak);
}
