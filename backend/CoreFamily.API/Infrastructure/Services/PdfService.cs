using CoreFamily.API.Application.Interfaces;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using Paragraph = iText.Layout.Element.Paragraph;

namespace CoreFamily.API.Infrastructure.Services;

/// <summary>
/// PDF service for generating certificates and reports
/// Install: dotnet add package iTextSharp (or iText7)
/// </summary>
public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(
        string recipientName,
        string programTitle,
        string certificateCode,
        DateTime completionDate,
        string instructorName = "Core Family")
    {
        try
        {
            // TODO: Implement using iTextSharp or iText7
            // Example structure:
            // var document = new Document(PageSize.A4);
            // var stream = new MemoryStream();
            // var writer = PdfWriter.GetInstance(document, stream);
            // document.Open();
            // ... add content ...
            // document.Close();
            // return stream.ToArray();

            _logger.LogInformation(
                "Generating certificate PDF for {User}: {Program} (Code: {Code})",
                recipientName,
                programTitle,
                certificateCode);

            var pdfContent = BuildCertificatePdf(
                recipientName,
                programTitle,
                certificateCode,
                completionDate,
                instructorName);

            return await Task.FromResult(pdfContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate PDF for {User}", recipientName);
            throw;
        }
    }

    public async Task<byte[]> GenerateProgressReportPdfAsync(
        string userName,
        List<(string ProgramTitle, DateTime CompletionDate)> completedPrograms,
        List<(string AchievementName, int Points)> unlockedAchievements,
        int currentStreak)
    {
        try
        {
            _logger.LogInformation(
                "Generating progress report PDF for {User}: {ProgramCount} programs, {AchievementCount} achievements",
                userName,
                completedPrograms.Count,
                unlockedAchievements.Count);

            var pdfContent = BuildProgressReportPdf(userName, completedPrograms, unlockedAchievements, currentStreak);
            return await Task.FromResult(pdfContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating progress report PDF for {User}", userName);
            throw;
        }
    }

    private static byte[] BuildCertificatePdf(
        string recipientName,
        string programTitle,
        string certificateCode,
        DateTime completionDate,
        string instructorName)
    {
        using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        document.SetMargins(60, 60, 60, 60);

        var titleColor = new DeviceRgb(37, 99, 235);

        document.Add(new Paragraph("Certificate of Completion")
            .SetFont(titleFont)
            .SetFontSize(30)
            .SetFontColor(titleColor)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(20));

        document.Add(new Paragraph("This certifies that")
            .SetFont(bodyFont)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));

        document.Add(new Paragraph(recipientName)
            .SetFont(titleFont)
            .SetFontSize(24)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));

        document.Add(new Paragraph("has successfully completed")
            .SetFont(bodyFont)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));

        document.Add(new Paragraph(programTitle)
            .SetFont(titleFont)
            .SetFontSize(20)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(25));

        document.Add(new Paragraph($"Completion Date: {completionDate:MMMM d, yyyy}")
            .SetFont(bodyFont)
            .SetFontSize(12)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));

        document.Add(new Paragraph($"Certificate Code: {certificateCode}")
            .SetFont(bodyFont)
            .SetFontSize(12)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));

        document.Add(new Paragraph($"Issued by: {instructorName}")
            .SetFont(bodyFont)
            .SetFontSize(12)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));

        document.Close();
        return stream.ToArray();
    }

    private static byte[] BuildProgressReportPdf(
        string userName,
        List<(string ProgramTitle, DateTime CompletionDate)> completedPrograms,
        List<(string AchievementName, int Points)> unlockedAchievements,
        int currentStreak)
    {
        using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        document.SetMargins(50, 50, 50, 50);

        document.Add(new Paragraph($"Progress Report: {userName}")
            .SetFont(titleFont)
            .SetFontSize(22)
            .SetTextAlignment(TextAlignment.LEFT)
            .SetMarginBottom(15));

        document.Add(new Paragraph($"Generated: {DateTime.UtcNow:MMMM d, yyyy h:mm tt} UTC")
            .SetFont(bodyFont)
            .SetFontSize(10)
            .SetMarginBottom(20));

        document.Add(new Paragraph($"Current Learning Streak: {currentStreak} days")
            .SetFont(titleFont)
            .SetFontSize(13)
            .SetMarginBottom(12));

        document.Add(new Paragraph($"Completed Programs ({completedPrograms.Count})")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginBottom(8));

        if (completedPrograms.Count == 0)
        {
            document.Add(new Paragraph("No completed programs yet.").SetFont(bodyFont).SetFontSize(11).SetMarginBottom(12));
        }
        else
        {
            foreach (var program in completedPrograms.OrderByDescending(p => p.CompletionDate))
            {
                document.Add(new Paragraph($"- {program.ProgramTitle} ({program.CompletionDate:MMMM d, yyyy})")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginBottom(3));
            }
            document.Add(new Paragraph("").SetMarginBottom(8));
        }

        document.Add(new Paragraph($"Unlocked Achievements ({unlockedAchievements.Count})")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginBottom(8));

        if (unlockedAchievements.Count == 0)
        {
            document.Add(new Paragraph("No unlocked achievements yet.").SetFont(bodyFont).SetFontSize(11));
        }
        else
        {
            foreach (var achievement in unlockedAchievements.OrderByDescending(a => a.Points))
            {
                document.Add(new Paragraph($"- {achievement.AchievementName} ({achievement.Points} points)")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginBottom(3));
            }
        }

        document.Close();
        return stream.ToArray();
    }
}
