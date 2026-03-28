using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class ProgramService : IProgramService
{
    private readonly CoreFamilyDbContext _db;
    private readonly IPaymentService _payments;

    public ProgramService(CoreFamilyDbContext db, IPaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    public async Task<IReadOnlyList<ProgramSummaryDto>> GetPublishedProgramsAsync(ProgramSearchDto search)
    {
        var query = _db.Programs
            .Include(p => p.Instructor).ThenInclude(i => i.Profile)
            .Include(p => p.Lessons)
            .Where(p => p.IsPublished)
            .AsQueryable();

        if (search.Category.HasValue)
        {
            query = query.Where(p => p.Category == search.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            var q = search.Query.Trim().ToLowerInvariant();
            query = query.Where(p => p.Title.ToLower().Contains(q) || p.Description.ToLower().Contains(q));
        }

        var programs = await query
            .OrderBy(p => p.Price)
            .ThenBy(p => p.Title)
            .ToListAsync();

        return programs.Select(MapProgramSummary).ToList();
    }

    public async Task<ProgramDetailDto?> GetPublishedProgramByIdAsync(Guid programId)
    {
        var program = await _db.Programs
            .Include(p => p.Instructor).ThenInclude(i => i.Profile)
            .Include(p => p.Lessons).ThenInclude(l => l.Content)
            .FirstOrDefaultAsync(p => p.Id == programId && p.IsPublished);

        if (program is null)
        {
            return null;
        }

        var lessons = program.Lessons
            .OrderBy(l => l.OrderIndex)
            .Select(l => new LessonSummaryDto(
                l.Id,
                l.Title,
                l.OrderIndex,
                l.IsRequired,
                l.Content.Type.ToString(),
                l.Content.IsFree,
                l.Content.Price))
            .ToList();

        return new ProgramDetailDto(
            program.Id,
            program.Title,
            program.Description,
            program.Price,
            program.DurationWeeks,
            program.Category,
            program.InstructorId,
            BuildFullName(program.Instructor.Profile?.FirstName, program.Instructor.Profile?.LastName),
            lessons);
    }

    public async Task<EnrollmentSummaryDto> EnrollAsync(Guid userId, Guid programId)
    {
        var program = await _db.Programs
            .Include(p => p.Lessons)
            .FirstOrDefaultAsync(p => p.Id == programId && p.IsPublished)
            ?? throw new KeyNotFoundException("Program not found or unpublished.");

        if (program.Price > 0)
        {
            var isPaid = await _payments.HasCompletedProgramPaymentAsync(userId, programId);
            if (!isPaid)
            {
                throw new InvalidOperationException("Payment required before enrollment for this program.");
            }
        }

        var exists = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.ProgramId == programId);
        if (exists)
        {
            throw new InvalidOperationException("You are already enrolled in this program.");
        }

        var enrollment = new Enrollment
        {
            UserId = userId,
            ProgramId = programId,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return new EnrollmentSummaryDto(
            enrollment.Id,
            program.Id,
            program.Title,
            enrollment.EnrolledAt,
            enrollment.CompletedAt,
            program.Lessons.Count,
            0);
    }

    public async Task<IReadOnlyList<EnrollmentSummaryDto>> GetMyEnrollmentsAsync(Guid userId)
    {
        var enrollments = await _db.Enrollments
            .Include(e => e.Program).ThenInclude(p => p.Lessons)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        if (enrollments.Count == 0)
        {
            return [];
        }

        var programIds = enrollments.Select(e => e.ProgramId).Distinct().ToArray();

        var completedByProgram = await _db.ProgressEntries
            .Include(pe => pe.Content)
            .Where(pe => pe.UserId == userId && pe.CompletedAt != null)
            .Join(_db.Lessons, pe => pe.ContentId, l => l.ContentId, (pe, l) => new { l.ProgramId, l.Id })
            .Where(x => programIds.Contains(x.ProgramId))
            .GroupBy(x => x.ProgramId)
            .Select(g => new { ProgramId = g.Key, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToDictionaryAsync(x => x.ProgramId, x => x.Count);

        return enrollments.Select(e => new EnrollmentSummaryDto(
            e.Id,
            e.ProgramId,
            e.Program.Title,
            e.EnrolledAt,
            e.CompletedAt,
            e.Program.Lessons.Count,
            completedByProgram.TryGetValue(e.ProgramId, out var done) ? done : 0
        )).ToList();
    }

    public async Task<ProgramLearningDto> GetLearningProgramAsync(Guid userId, Guid programId)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Program).ThenInclude(p => p.Lessons).ThenInclude(l => l.Content)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ProgramId == programId)
            ?? throw new KeyNotFoundException("Enrollment not found for this program.");

        var progressEntries = await _db.ProgressEntries
            .Where(pe => pe.UserId == userId)
            .ToDictionaryAsync(pe => pe.ContentId, pe => pe);

        var lessons = enrollment.Program.Lessons
            .OrderBy(l => l.OrderIndex)
            .Select(l => MapLessonPlayer(l, progressEntries.TryGetValue(l.ContentId, out var progress) ? progress : null))
            .ToList();

        var completedLessons = lessons.Count(l => l.CompletedAt != null);

        if (completedLessons == lessons.Count && lessons.Count > 0 && enrollment.CompletedAt is null)
        {
            enrollment.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return new ProgramLearningDto(
            enrollment.ProgramId,
            enrollment.Program.Title,
            enrollment.EnrolledAt,
            enrollment.CompletedAt,
            lessons.Count,
            completedLessons,
            lessons);
    }

    public async Task<LessonPlayerDto> GetLessonAsync(Guid userId, Guid programId, Guid lessonId)
    {
        await EnsureEnrollmentAsync(userId, programId);

        var lesson = await _db.Lessons
            .Include(l => l.Content)
            .FirstOrDefaultAsync(l => l.ProgramId == programId && l.Id == lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        var progress = await _db.ProgressEntries
            .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.ContentId == lesson.ContentId);

        return MapLessonPlayer(lesson, progress);
    }

    public async Task<LessonPlayerDto> UpdateLessonProgressAsync(Guid userId, Guid programId, Guid lessonId, UpdateLessonProgressDto dto)
    {
        await EnsureEnrollmentAsync(userId, programId);

        var lesson = await _db.Lessons
            .Include(l => l.Content)
            .FirstOrDefaultAsync(l => l.ProgramId == programId && l.Id == lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        var progress = await _db.ProgressEntries
            .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.ContentId == lesson.ContentId);

        if (progress is null)
        {
            progress = new ProgressEntry
            {
                UserId = userId,
                ContentId = lesson.ContentId
            };
            _db.ProgressEntries.Add(progress);
        }

        progress.SecondsWatched = Math.Max(progress.SecondsWatched, dto.SecondsWatched);
        if (dto.MarkCompleted)
        {
            progress.CompletedAt = progress.CompletedAt ?? DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await UpdateEnrollmentCompletionIfNeeded(userId, programId);
        return MapLessonPlayer(lesson, progress);
    }

    public async Task<IReadOnlyList<InstructorProgramSummaryDto>> GetMyProgramsAsync(Guid instructorUserId)
    {
        await EnsureInstructorAsync(instructorUserId);

        var programs = await _db.Programs
            .Include(p => p.Lessons)
            .Where(p => p.InstructorId == instructorUserId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return programs.Select(p => new InstructorProgramSummaryDto(
            p.Id,
            p.Title,
            p.Description,
            p.Price,
            p.DurationWeeks,
            p.Category,
            p.IsPublished,
            p.Lessons.Count,
            p.UpdatedAt
        )).ToList();
    }

    public async Task<InstructorProgramSummaryDto> CreateProgramAsync(Guid instructorUserId, InstructorProgramUpsertDto dto)
    {
        await EnsureInstructorAsync(instructorUserId);

        var program = new Program_
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            DurationWeeks = dto.DurationWeeks,
            Category = dto.Category,
            InstructorId = instructorUserId,
            IsPublished = false
        };

        _db.Programs.Add(program);
        await _db.SaveChangesAsync();

        return new InstructorProgramSummaryDto(
            program.Id,
            program.Title,
            program.Description,
            program.Price,
            program.DurationWeeks,
            program.Category,
            program.IsPublished,
            0,
            program.UpdatedAt);
    }

    public async Task<InstructorProgramSummaryDto> UpdateProgramAsync(Guid instructorUserId, Guid programId, InstructorProgramUpsertDto dto)
    {
        var program = await GetOwnedProgramAsync(instructorUserId, programId);

        program.Title = dto.Title.Trim();
        program.Description = dto.Description.Trim();
        program.Price = dto.Price;
        program.DurationWeeks = dto.DurationWeeks;
        program.Category = dto.Category;

        await _db.SaveChangesAsync();

        var lessonCount = await _db.Lessons.CountAsync(l => l.ProgramId == programId);
        return new InstructorProgramSummaryDto(
            program.Id,
            program.Title,
            program.Description,
            program.Price,
            program.DurationWeeks,
            program.Category,
            program.IsPublished,
            lessonCount,
            program.UpdatedAt);
    }

    public async Task<InstructorProgramSummaryDto> PublishProgramAsync(Guid instructorUserId, Guid programId)
    {
        var program = await GetOwnedProgramAsync(instructorUserId, programId);
        var lessonCount = await _db.Lessons.CountAsync(l => l.ProgramId == programId);

        if (lessonCount == 0)
        {
            throw new InvalidOperationException("A program must contain at least one lesson before publishing.");
        }

        program.IsPublished = true;
        await _db.SaveChangesAsync();

        return new InstructorProgramSummaryDto(
            program.Id,
            program.Title,
            program.Description,
            program.Price,
            program.DurationWeeks,
            program.Category,
            program.IsPublished,
            lessonCount,
            program.UpdatedAt);
    }

    public async Task<InstructorLessonSummaryDto> AddLessonAsync(Guid instructorUserId, Guid programId, InstructorLessonUpsertDto dto)
    {
        var program = await GetOwnedProgramAsync(instructorUserId, programId);

        var slugBase = $"{program.Title}-{dto.ContentTitle}-{Guid.NewGuid():N}".ToLowerInvariant().Replace(' ', '-');
        var content = new Content
        {
            Title = dto.ContentTitle.Trim(),
            Slug = slugBase,
            Description = dto.ContentDescription?.Trim(),
            Body = dto.ContentBody,
            Type = dto.ContentType,
            Category = program.Category,
            IsFree = dto.IsFree,
            Price = dto.IsFree ? 0 : dto.Price,
            ThumbnailUrl = dto.ThumbnailUrl,
            CreatedById = instructorUserId,
            IsPublished = true
        };

        var lesson = new Lesson
        {
            ProgramId = programId,
            Title = dto.Title.Trim(),
            OrderIndex = dto.OrderIndex,
            Content = content,
            IsRequired = dto.IsRequired
        };

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        return new InstructorLessonSummaryDto(
            lesson.Id,
            lesson.ProgramId,
            lesson.ContentId,
            lesson.Title,
            lesson.OrderIndex,
            lesson.IsRequired,
            content.Type,
            content.IsFree,
            content.Price,
            content.IsPublished);
    }

    public async Task<InstructorLessonSummaryDto> UpdateLessonAsync(Guid instructorUserId, Guid programId, Guid lessonId, InstructorLessonUpsertDto dto)
    {
        await GetOwnedProgramAsync(instructorUserId, programId);

        var lesson = await _db.Lessons
            .Include(l => l.Content)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.ProgramId == programId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        lesson.Title = dto.Title.Trim();
        lesson.OrderIndex = dto.OrderIndex;
        lesson.IsRequired = dto.IsRequired;

        lesson.Content.Title = dto.ContentTitle.Trim();
        lesson.Content.Description = dto.ContentDescription?.Trim();
        lesson.Content.Body = dto.ContentBody;
        lesson.Content.Type = dto.ContentType;
        lesson.Content.IsFree = dto.IsFree;
        lesson.Content.Price = dto.IsFree ? 0 : dto.Price;
        lesson.Content.ThumbnailUrl = dto.ThumbnailUrl;

        await _db.SaveChangesAsync();

        return new InstructorLessonSummaryDto(
            lesson.Id,
            lesson.ProgramId,
            lesson.ContentId,
            lesson.Title,
            lesson.OrderIndex,
            lesson.IsRequired,
            lesson.Content.Type,
            lesson.Content.IsFree,
            lesson.Content.Price,
            lesson.Content.IsPublished);
    }

    private async Task EnsureInstructorAsync(Guid instructorUserId)
    {
        var isInstructor = await _db.UserRoles.AnyAsync(r => r.UserId == instructorUserId && r.Role == UserRole.Instructor);
        if (!isInstructor)
        {
            throw new UnauthorizedAccessException("Only instructor accounts can manage programs.");
        }
    }

    private async Task<Program_> GetOwnedProgramAsync(Guid instructorUserId, Guid programId)
    {
        await EnsureInstructorAsync(instructorUserId);

        return await _db.Programs
            .FirstOrDefaultAsync(p => p.Id == programId && p.InstructorId == instructorUserId)
            ?? throw new KeyNotFoundException("Program not found for this instructor.");
    }

    private async Task EnsureEnrollmentAsync(Guid userId, Guid programId)
    {
        var isEnrolled = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.ProgramId == programId);
        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException("You must be enrolled in this program to access lessons.");
        }
    }

    private async Task UpdateEnrollmentCompletionIfNeeded(Guid userId, Guid programId)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Program).ThenInclude(p => p.Lessons)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ProgramId == programId);

        if (enrollment is null)
        {
            return;
        }

        var lessonContentIds = enrollment.Program.Lessons.Select(l => l.ContentId).ToArray();
        if (lessonContentIds.Length == 0)
        {
            return;
        }

        var completedCount = await _db.ProgressEntries
            .Where(pe => pe.UserId == userId && pe.CompletedAt != null && lessonContentIds.Contains(pe.ContentId))
            .Select(pe => pe.ContentId)
            .Distinct()
            .CountAsync();

        if (completedCount == lessonContentIds.Length && enrollment.CompletedAt is null)
        {
            enrollment.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static LessonPlayerDto MapLessonPlayer(Lesson lesson, ProgressEntry? progress) => new(
        lesson.Id,
        lesson.ProgramId,
        lesson.ContentId,
        lesson.Title,
        lesson.OrderIndex,
        lesson.IsRequired,
        lesson.Content.Title,
        lesson.Content.Description,
        lesson.Content.Body,
        lesson.Content.Type.ToString(),
        lesson.Content.IsFree,
        lesson.Content.Price,
        progress?.SecondsWatched ?? 0,
        progress?.CompletedAt);

    private static ProgramSummaryDto MapProgramSummary(Program_ program) => new(
        program.Id,
        program.Title,
        program.Description,
        program.Price,
        program.DurationWeeks,
        program.Category,
        BuildFullName(program.Instructor.Profile?.FirstName, program.Instructor.Profile?.LastName),
        program.Lessons.Count);

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Unknown Instructor" : fullName;
    }

    public async Task<ProgressSummaryDto> GetProgressSummaryAsync(Guid userId)
    {
        var enrollments = await _db.Enrollments
            .Include(e => e.Program).ThenInclude(p => p.Lessons)
            .Where(e => e.UserId == userId)
            .ToListAsync();

        if (enrollments.Count == 0)
        {
            return new ProgressSummaryDto(
                TotalEnrollments: 0,
                CompletedPrograms: 0,
                InProgressPrograms: 0,
                CompletionPercentage: 0,
                TotalLessonsCompleted: 0,
                TotalLessonsEnrolled: 0,
                MostRecentCompletionDate: null,
                Enrollments: []
            );
        }

        var enrollmentDtos = new List<EnrollmentSummaryDto>();
        int totalCompleted = 0;
        int completedPrograms = 0;
        DateTime? mostRecentCompletion = null;

        foreach (var enrollment in enrollments)
        {
            var lessonContentIds = enrollment.Program.Lessons.Select(l => l.ContentId).ToArray();
            var completedCount = 0;

            if (lessonContentIds.Length > 0)
            {
                completedCount = await _db.ProgressEntries
                    .Where(pe => pe.UserId == userId && pe.CompletedAt != null && lessonContentIds.Contains(pe.ContentId))
                    .Select(pe => pe.ContentId)
                    .Distinct()
                    .CountAsync();
            }

            totalCompleted += completedCount;
            if (enrollment.CompletedAt.HasValue)
            {
                completedPrograms++;
                if (mostRecentCompletion == null || enrollment.CompletedAt > mostRecentCompletion)
                {
                    mostRecentCompletion = enrollment.CompletedAt;
                }
            }

            enrollmentDtos.Add(new EnrollmentSummaryDto(
                enrollment.Id,
                enrollment.Program.Id,
                enrollment.Program.Title,
                enrollment.EnrolledAt,
                enrollment.CompletedAt,
                enrollment.Program.Lessons.Count,
                completedCount
            ));
        }

        var totalLessonsEnrolled = enrollments.SelectMany(e => e.Program.Lessons).Distinct().Count();
        var completionPercentage = totalLessonsEnrolled > 0 ? (decimal)totalCompleted / totalLessonsEnrolled * 100 : 0;

        return new ProgressSummaryDto(
            TotalEnrollments: enrollments.Count,
            CompletedPrograms: completedPrograms,
            InProgressPrograms: enrollments.Count - completedPrograms,
            CompletionPercentage: Math.Round(completionPercentage, 2),
            TotalLessonsCompleted: totalCompleted,
            TotalLessonsEnrolled: totalLessonsEnrolled,
            MostRecentCompletionDate: mostRecentCompletion,
            Enrollments: enrollmentDtos
        );
    }

    public async Task<CertificateDto> GenerateCertificateAsync(Guid userId, Guid programId)
    {
        // Check if enrollment exists and is completed
        var enrollment = await _db.Enrollments
            .Include(e => e.Program)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ProgramId == programId)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        if (enrollment.CompletedAt is null)
        {
            throw new InvalidOperationException("Program must be completed before generating a certificate.");
        }

        // Check if certificate already exists
        var existing = await _db.Certificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProgramId == programId);

        if (existing is not null)
        {
            return MapCertificate(existing, enrollment.Program.Title);
        }

        // Generate certificate
        var certificate = new Certificate
        {
            UserId = userId,
            ProgramId = programId,
            CertificateCode = GenerateCertificateCode(),
            PdfUrl = await GenerateCertificatePdfAsync(userId, enrollment.Program, enrollment.CompletedAt.Value),
            IssuedAt = DateTime.UtcNow
        };

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync();

        return MapCertificate(certificate, enrollment.Program.Title);
    }

    public async Task<IReadOnlyList<CertificateDto>> GetMyCertificatesAsync(Guid userId)
    {
        var certificates = await _db.Certificates
            .Include(c => c.Program)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        return certificates.Select(c => MapCertificate(c, c.Program?.Title ?? "Unknown Program")).ToList();
    }

    public async Task<CertificateDto?> GetCertificateByIdAsync(Guid certificateId)
    {
        var cert = await _db.Certificates
            .Include(c => c.Program)
            .FirstOrDefaultAsync(c => c.Id == certificateId);

        return cert is null ? null : MapCertificate(cert, cert.Program?.Title ?? "Unknown Program");
    }

    private static string GenerateCertificateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[12];
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        return new string(code);
    }

    private static Task<string> GenerateCertificatePdfAsync(Guid userId, Program_ program, DateTime completedAt)
    {
        // TODO: Implement actual PDF generation (e.g., using iTextSharp or similar)
        // For now, return a placeholder URL
        var certificateId = Guid.NewGuid();
        var url = $"https://certificates.corefamily.edu/{certificateId}/download";
        return Task.FromResult(url);
    }

    private static CertificateDto MapCertificate(Certificate cert, string? programTitle) => new(
        cert.Id,
        cert.UserId,
        cert.ProgramId,
        cert.CertificateCode,
        cert.PdfUrl,
        cert.IssuedAt,
        programTitle
    );
}
