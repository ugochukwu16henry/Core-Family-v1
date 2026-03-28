namespace CoreFamily.API.Application.Interfaces;

public interface IEmailService
{
    Task SendAchievementUnlockedAsync(string email, string userName, string achievementName, int points);
    Task SendCertificateIssuedAsync(string email, string userName, string programTitle, string certificateCode);
    Task SendMilestoneReachedAsync(string email, string userName, int programsCompleted);
    Task SendStreakNotificationAsync(string email, string userName, int streakDays);
}
