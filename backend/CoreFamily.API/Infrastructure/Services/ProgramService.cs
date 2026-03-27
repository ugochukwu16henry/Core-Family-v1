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
}
