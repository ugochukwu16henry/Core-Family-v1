using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class ProgramService : IProgramService
{
    private readonly CoreFamilyDbContext _db;

    public ProgramService(CoreFamilyDbContext db) => _db = db;

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
