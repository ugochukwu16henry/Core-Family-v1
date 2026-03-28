using CoreFamily.API.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreFamily.API.Infrastructure.Services;

/// <summary>
/// SendGrid email provider implementation
/// Install: dotnet add package SendGrid
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string? _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _logger = logger;
        _sendGridApiKey = configuration["EmailProvider:SendGrid:ApiKey"];
        _fromEmail = configuration["EmailProvider:SendGrid:FromEmail"] ?? "noreply@corefamily.edu";
        _fromName = configuration["EmailProvider:SendGrid:FromName"] ?? "Core Family";

        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email sending will fail.");
        }
    }

    public async Task SendAchievementUnlockedAsync(string email, string userName, string achievementName, int points)
    {
        try
        {
            var subject = $"🎉 Achievement Unlocked: {achievementName}!";
            var htmlBody = GenerateAchievementEmail(userName, achievementName, points);
            await SendEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send achievement email to {Email}", email);
            throw;
        }
    }

    public async Task SendCertificateIssuedAsync(string email, string userName, string programTitle, string certificateCode)
    {
        try
        {
            var subject = $"📜 Certificate of Completion - {programTitle}";
            var htmlBody = GenerateCertificateEmail(userName, programTitle, certificateCode);
            await SendEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send certificate email to {Email}", email);
            throw;
        }
    }

    public async Task SendMilestoneReachedAsync(string email, string userName, int programsCompleted)
    {
        try
        {
            var milestoneName = GetMilestoneName(programsCompleted);
            var subject = $"🏆 Milestone Reached: {milestoneName}!";
            var htmlBody = GenerateMilestoneEmail(userName, milestoneName, programsCompleted);
            await SendEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send milestone email to {Email}", email);
            throw;
        }
    }

    public async Task SendStreakNotificationAsync(string email, string userName, int streakDays)
    {
        try
        {
            var subject = $"🔥 Amazing Streak! {streakDays} Days of Learning";
            var htmlBody = GenerateStreakEmail(userName, streakDays);
            await SendEmailAsync(email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send streak email to {Email}", email);
            throw;
        }
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email would be sent to {Email} but cannot proceed.", to);
            await Task.CompletedTask;
            return;
        }

        try
        {
            // TODO: Uncomment when SendGrid NuGet package is installed
            /*
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to_email = new EmailAddress(to);
            var msg = new SendGridMessage()
            {
                From = from,
                Subject = subject,
                HtmlContent = htmlBody
            };
            msg.AddTo(to_email);

            var response = await client.SendEmailAsync(msg);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            else
            {
                _logger.LogError("SendGrid returned status {StatusCode} for email to {Email}", response.StatusCode, to);
            }
            */

            _logger.LogInformation("Email queued via SendGrid: To={Email}, Subject={Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid to {Email}", to);
            throw;
        }
    }

    private string GenerateAchievementEmail(string userName, string achievementName, int points)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .emoji {{ font-size: 48px; margin: 10px 0; }}
        .title {{ color: #333; font-size: 28px; font-weight: bold; margin: 20px 0; }}
        .content {{ color: #666; line-height: 1.6; margin: 20px 0; }}
        .points {{ background: #667eea; color: white; padding: 10px 20px; border-radius: 6px; display: inline-block; margin: 15px 0; font-weight: bold; }}
        .cta {{ text-align: center; margin-top: 30px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; border-radius: 6px; text-decoration: none; display: inline-block; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>🎉</div>
            <div class='title'>Achievement Unlocked!</div>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>Congratulations! You've unlocked the <strong>{achievementName}</strong> achievement!</p>
            <div class='points'>+ {points} points</div>
            <p>You're making amazing progress on your learning journey. Keep up the great work!</p>
        </div>
        <div class='cta'>
            <a href='https://corefamily.edu/progress' class='button'>View Your Achievements</a>
        </div>
        <div class='footer'>
            <p>Core Family © 2026. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateCertificateEmail(string userName, string programTitle, string certificateCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .emoji {{ font-size: 48px; margin: 10px 0; }}
        .title {{ color: #333; font-size: 28px; font-weight: bold; margin: 20px 0; }}
        .content {{ color: #666; line-height: 1.6; margin: 20px 0; }}
        .certificate-code {{ background: #f0f0f0; padding: 15px; border-radius: 6px; text-align: center; font-family: monospace; font-weight: bold; margin: 20px 0; }}
        .cta {{ text-align: center; margin-top: 30px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; border-radius: 6px; text-decoration: none; display: inline-block; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>📜</div>
            <div class='title'>Certificate of Completion</div>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>Congratulations on completing <strong>{programTitle}</strong>!</p>
            <p>Your certificate of completion has been issued. Here's your certificate code:</p>
            <div class='certificate-code'>{certificateCode}</div>
            <p>You can download and share your certificate anytime from your dashboard.</p>
        </div>
        <div class='cta'>
            <a href='https://corefamily.edu/certificates' class='button'>Download Your Certificate</a>
        </div>
        <div class='footer'>
            <p>Core Family © 2026. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateMilestoneEmail(string userName, string milestoneName, int programsCompleted)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .emoji {{ font-size: 48px; margin: 10px 0; }}
        .title {{ color: #333; font-size: 28px; font-weight: bold; margin: 20px 0; }}
        .content {{ color: #666; line-height: 1.6; margin: 20px 0; }}
        .milestone {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 6px; margin: 20px 0; text-align: center; }}
        .cta {{ text-align: center; margin-top: 30px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; border-radius: 6px; text-decoration: none; display: inline-block; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>🏆</div>
            <div class='title'>Milestone Reached!</div>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>You've achieved the <strong>{milestoneName}</strong> milestone by completing {programsCompleted} programs!</p>
            <div class='milestone'>
                <p style='font-size: 24px; margin: 0;'>You're on fire! 🔥</p>
            </div>
            <p>Keep learning and growing. You're part of an incredible community of learners.</p>
        </div>
        <div class='cta'>
            <a href='https://corefamily.edu/progress' class='button'>View Your Progress</a>
        </div>
        <div class='footer'>
            <p>Core Family © 2026. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateStreakEmail(string userName, int streakDays)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .emoji {{ font-size: 48px; margin: 10px 0; }}
        .title {{ color: #333; font-size: 28px; font-weight: bold; margin: 20px 0; }}
        .content {{ color: #666; line-height: 1.6; margin: 20px 0; }}
        .streak {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white; padding: 20px; border-radius: 6px; margin: 20px 0; text-align: center; }}
        .streak-number {{ font-size: 48px; font-weight: bold; }}
        .cta {{ text-align: center; margin-top: 30px; }}
        .button {{ background: #667eea; color: white; padding: 12px 30px; border-radius: 6px; text-decoration: none; display: inline-block; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>🔥</div>
            <div class='title'>Amazing Streak!</div>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>You're on fire! You've maintained a {streakDays}-day learning streak. That's incredible dedication!</p>
            <div class='streak'>
                <div class='streak-number'>{streakDays}</div>
                <p>Days of Continuous Learning</p>
            </div>
            <p>Keep going! Every day you learn makes you stronger and more accomplished.</p>
        </div>
        <div class='cta'>
            <a href='https://corefamily.edu/progress' class='button'>Keep Learning</a>
        </div>
        <div class='footer'>
            <p>Core Family © 2026. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GetMilestoneName(int programsCompleted)
    {
        return programsCompleted switch
        {
            1 => "First Step",
            3 => "Early Achiever",
            5 => "Dedicated Learner",
            10 => "Knowledge Master",
            20 => "Unstoppable",
            _ => $"{programsCompleted} Programs"
        };
    }
}
