using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class CounselorService : ICounselorService
{
    private const decimal PlatformCommissionRate = 0.05m;
    private readonly CoreFamilyDbContext _db;
    private readonly IPaymentService _payments;

    public CounselorService(CoreFamilyDbContext db, IPaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    public async Task<IReadOnlyList<CounselorSummaryDto>> SearchAsync(CounselorSearchDto search)
    {
        var query = _db.CounselorProfiles
            .Include(cp => cp.User).ThenInclude(u => u.Profile)
            .Include(cp => cp.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Language))
        {
            var language = search.Language.Trim().ToLowerInvariant();
            query = query.Where(cp => cp.Languages.ToLower().Contains(language));
        }

        if (!string.IsNullOrWhiteSpace(search.Specialization))
        {
            var specialization = search.Specialization.Trim().ToLowerInvariant();
            query = query.Where(cp => cp.Specialization != null && cp.Specialization.ToLower().Contains(specialization));
        }

        if (!string.IsNullOrWhiteSpace(search.Country))
        {
            var country = search.Country.Trim().ToLowerInvariant();
            query = query.Where(cp => cp.User.Profile != null && cp.User.Profile.Country != null && cp.User.Profile.Country.ToLower() == country);
        }

        if (search.AcceptsNewClients.HasValue)
        {
            query = query.Where(cp => cp.AcceptsNewClients == search.AcceptsNewClients.Value);
        }

        var counselors = await query
            .OrderByDescending(cp => cp.LicenseStatus == VerificationStatus.Verified)
            .ThenBy(cp => cp.User.Profile!.FirstName)
            .ToListAsync();

        return counselors.Select(MapCounselorSummary).ToList();
    }

    public async Task<CounselorSummaryDto?> GetByIdAsync(Guid counselorId)
    {
        var counselor = await _db.CounselorProfiles
            .Include(cp => cp.User).ThenInclude(u => u.Profile)
            .Include(cp => cp.Reviews)
            .FirstOrDefaultAsync(cp => cp.Id == counselorId);

        return counselor is null ? null : MapCounselorSummary(counselor);
    }

    public async Task<CounselorSummaryDto> UpsertMyProfileAsync(Guid userId, CounselorProfileUpsertDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!user.Roles.Any(r => r.Role == UserRole.Counselor))
            throw new UnauthorizedAccessException("Only counselor accounts can manage counselor profiles.");

        var profile = await _db.CounselorProfiles
            .Include(cp => cp.User).ThenInclude(u => u.Profile)
            .Include(cp => cp.Reviews)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile is null)
        {
            profile = new CounselorProfile
            {
                UserId = userId,
                User = user,
                LicenseStatus = string.IsNullOrWhiteSpace(dto.LicenseUrl)
                    ? VerificationStatus.NotSubmitted
                    : VerificationStatus.Pending
            };

            _db.CounselorProfiles.Add(profile);
        }

        profile.LicenseUrl = dto.LicenseUrl?.Trim();
        profile.QualificationsUrl = dto.QualificationsUrl?.Trim();
        profile.LicenseExpiryDate = dto.LicenseExpiryDate;
        profile.Specialization = dto.Specialization?.Trim();
        profile.HourlyRateUsd = dto.HourlyRateUsd;
        profile.Languages = string.Join(',', dto.Languages
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase));
        profile.AvailabilityJson = dto.AvailabilityJson;
        profile.AcceptsNewClients = dto.AcceptsNewClients;

        if (!string.IsNullOrWhiteSpace(dto.LicenseUrl) && profile.LicenseStatus == VerificationStatus.NotSubmitted)
            profile.LicenseStatus = VerificationStatus.Pending;

        await _db.SaveChangesAsync();
        return MapCounselorSummary(profile);
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> GetCounselorSessionsAsync(Guid userId)
    {
        var counselorId = await _db.CounselorProfiles
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.Id)
            .FirstOrDefaultAsync();

        if (counselorId == Guid.Empty)
            return [];

        var sessions = await QuerySessions()
            .Where(s => s.CounselorId == counselorId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

        return await MapSessionSummariesAsync(sessions);
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> GetClientSessionsAsync(Guid userId)
    {
        var sessions = await QuerySessions()
            .Where(s => s.ClientId == userId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

        return await MapSessionSummariesAsync(sessions);
    }

    public async Task<SessionSummaryDto?> GetSessionByIdAsync(Guid userId, Guid sessionId)
    {
        var session = await QuerySessions().FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var counselorUserId = session.Counselor.UserId;
        if (session.ClientId != userId && counselorUserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this session.");
        }

        return await MapSessionSummaryAsync(session);
    }

    public async Task<SessionSummaryDto> BookSessionAsync(Guid clientUserId, BookSessionDto dto)
    {
        var client = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == clientUserId)
            ?? throw new KeyNotFoundException("Client not found.");

        var counselor = await _db.CounselorProfiles
            .Include(cp => cp.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(cp => cp.Id == dto.CounselorId)
            ?? throw new KeyNotFoundException("Counselor not found.");

        if (!counselor.AcceptsNewClients)
            throw new InvalidOperationException("This counselor is not accepting new clients.");

        if (counselor.UserId == clientUserId)
            throw new InvalidOperationException("You cannot book a session with yourself.");

        var start = dto.ScheduledAt.ToUniversalTime();
        var hasConflict = await HasCounselorConflictAsync(counselor.Id, start, dto.DurationMinutes, null);

        if (hasConflict)
            throw new InvalidOperationException("The selected time conflicts with an existing counselor session.");

        var amountPaid = Math.Round(counselor.HourlyRateUsd * dto.DurationMinutes / 60m, 2, MidpointRounding.AwayFromZero);
        var platformCommission = Math.Round(amountPaid * PlatformCommissionRate, 2, MidpointRounding.AwayFromZero);

        var session = new Session
        {
            CounselorId = counselor.Id,
            ClientId = clientUserId,
            ScheduledAt = start,
            DurationMinutes = dto.DurationMinutes,
            Status = SessionStatus.Pending,
            AmountPaid = amountPaid,
            PlatformCommission = platformCommission,
            Notes = dto.Notes?.Trim()
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        return await MapSessionSummaryAsync(session);
    }

    public async Task<SessionSummaryDto> ConfirmSessionAsync(Guid counselorUserId, Guid sessionId)
    {
        var session = await QuerySessions().FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.Counselor.UserId != counselorUserId)
            throw new UnauthorizedAccessException("Only the assigned counselor can confirm this session.");

        if (session.Status is SessionStatus.Cancelled or SessionStatus.Completed)
            throw new InvalidOperationException("This session can no longer be confirmed.");

        if (session.AmountPaid > 0)
        {
            var isPaid = await _payments.HasCompletedSessionPaymentAsync(session.ClientId, session.Id);
            if (!isPaid)
                throw new InvalidOperationException("Payment must be completed before confirming this session.");
        }

        session.Status = SessionStatus.Confirmed;
        await _db.SaveChangesAsync();
        return await MapSessionSummaryAsync(session);
    }

    public async Task<SessionSummaryDto> CancelSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await QuerySessions().FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.ClientId != userId && session.Counselor.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to cancel this session.");

        if (session.Status is SessionStatus.Completed or SessionStatus.Cancelled)
            throw new InvalidOperationException("This session cannot be cancelled.");

        session.Status = SessionStatus.Cancelled;
        await _db.SaveChangesAsync();
        return await MapSessionSummaryAsync(session);
    }

    public async Task<SessionSummaryDto> RescheduleSessionAsync(Guid userId, Guid sessionId, RescheduleSessionDto dto)
    {
        var session = await QuerySessions().FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.ClientId != userId && session.Counselor.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to reschedule this session.");

        if (session.Status is SessionStatus.Completed or SessionStatus.Cancelled)
            throw new InvalidOperationException("This session cannot be rescheduled.");

        var start = dto.ScheduledAt.ToUniversalTime();
        var hasConflict = await HasCounselorConflictAsync(session.CounselorId, start, dto.DurationMinutes, session.Id);
        if (hasConflict)
            throw new InvalidOperationException("The selected time conflicts with an existing counselor session.");

        session.ScheduledAt = start;
        session.DurationMinutes = dto.DurationMinutes;
        session.Notes = dto.Notes?.Trim();
        session.Status = SessionStatus.Pending;

        await _db.SaveChangesAsync();
        return await MapSessionSummaryAsync(session);
    }

    private IQueryable<Session> QuerySessions() => _db.Sessions
        .Include(s => s.Counselor).ThenInclude(c => c.User).ThenInclude(u => u.Profile)
        .Include(s => s.Client).ThenInclude(u => u.Profile);

    private static CounselorSummaryDto MapCounselorSummary(CounselorProfile counselor)
    {
        var profile = counselor.User.Profile ?? throw new InvalidOperationException("Counselor user profile is missing.");
        var ratings = counselor.Reviews.Where(r => r.Rating > 0).Select(r => r.Rating).ToList();
        var averageRating = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 2, MidpointRounding.AwayFromZero);

        return new CounselorSummaryDto(
            counselor.Id,
            counselor.UserId,
            profile.FirstName,
            profile.LastName,
            profile.Bio,
            profile.Country,
            profile.City,
            profile.PreferredLanguage,
            counselor.Specialization,
            counselor.HourlyRateUsd,
            SplitLanguages(counselor.Languages),
            counselor.AcceptsNewClients,
            counselor.LicenseStatus.ToString(),
            averageRating,
            ratings.Count);
    }

    private async Task<IReadOnlyList<SessionSummaryDto>> MapSessionSummariesAsync(IReadOnlyList<Session> sessions)
    {
        if (sessions.Count == 0)
            return [];

        var sessionIds = sessions.Select(s => s.Id).ToArray();
        var paidSessions = await _db.Transactions
            .Where(t => t.Type == TransactionType.CounselingSession && t.ReferenceId.HasValue && sessionIds.Contains(t.ReferenceId.Value))
            .GroupBy(t => t.ReferenceId!.Value)
            .Select(g => new
            {
                SessionId = g.Key,
                IsPaid = g.Any(x => x.Status == TransactionStatus.Completed),
                PaymentStatus = g.OrderByDescending(x => x.CreatedAt).Select(x => x.Status).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.SessionId, x => (x.IsPaid, x.PaymentStatus.ToString()));

        return sessions.Select(s => MapSessionSummary(s, paidSessions.TryGetValue(s.Id, out var p) ? p : (false, "Pending"))).ToList();
    }

    private async Task<SessionSummaryDto> MapSessionSummaryAsync(Session session)
    {
        var isPaid = await _payments.HasCompletedSessionPaymentAsync(session.ClientId, session.Id);
        var paymentStatus = await _db.Transactions
            .Where(t => t.Type == TransactionType.CounselingSession && t.ReferenceId == session.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.Status.ToString())
            .FirstOrDefaultAsync() ?? (isPaid ? "Completed" : "Pending");

        return MapSessionSummary(session, (isPaid, paymentStatus));
    }

    private static SessionSummaryDto MapSessionSummary(Session session, (bool IsPaid, string PaymentStatus) payment) => new(
        session.Id,
        session.CounselorId,
        session.ClientId,
        BuildFullName(session.Counselor.User.Profile?.FirstName, session.Counselor.User.Profile?.LastName),
        BuildFullName(session.Client.Profile?.FirstName, session.Client.Profile?.LastName),
        session.ScheduledAt,
        session.DurationMinutes,
        session.Status.ToString(),
        session.AmountPaid,
        session.PlatformCommission,
        payment.IsPaid,
        payment.PaymentStatus,
        session.Notes,
        session.MeetingUrl);

    private async Task<bool> HasCounselorConflictAsync(Guid counselorId, DateTime start, int durationMinutes, Guid? sessionToIgnore)
    {
        var end = start.AddMinutes(durationMinutes);
        var existingSessions = await _db.Sessions
            .Where(s => s.CounselorId == counselorId && s.Status != SessionStatus.Cancelled && (!sessionToIgnore.HasValue || s.Id != sessionToIgnore.Value))
            .ToListAsync();

        return existingSessions.Any(s =>
        {
            var existingStart = s.ScheduledAt;
            var existingEnd = existingStart.AddMinutes(s.DurationMinutes);
            return start < existingEnd && end > existingStart;
        });
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Unknown User" : fullName;
    }

    private static string[] SplitLanguages(string rawLanguages) => rawLanguages
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}