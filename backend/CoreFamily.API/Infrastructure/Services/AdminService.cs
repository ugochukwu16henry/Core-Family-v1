using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly CoreFamilyDbContext _db;

    public AdminService(CoreFamilyDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminUserSummaryDto>> GetUsersAsync()
    {
        var users = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(u => new AdminUserSummaryDto(
            u.Id,
            u.Email,
            u.Profile?.FirstName ?? string.Empty,
            u.Profile?.LastName ?? string.Empty,
            u.Roles.FirstOrDefault()?.Role.ToString() ?? "Client",
            u.Profile?.Category.ToString() ?? "Single",
            u.IsActive,
            u.EmailVerified,
            u.CreatedAt,
            u.LastLoginAt)).ToList();
    }

    public async Task<AdminUserSummaryDto> SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync();

        return new AdminUserSummaryDto(
            user.Id,
            user.Email,
            user.Profile?.FirstName ?? string.Empty,
            user.Profile?.LastName ?? string.Empty,
            user.Roles.FirstOrDefault()?.Role.ToString() ?? "Client",
            user.Profile?.Category.ToString() ?? "Single",
            user.IsActive,
            user.EmailVerified,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<IReadOnlyList<AdminReviewSummaryDto>> GetFlaggedReviewsAsync()
    {
        var reviews = await _db.Reviews
            .Include(r => r.Reviewer).ThenInclude(u => u.Profile)
            .Include(r => r.Counselor!).ThenInclude(c => c.User).ThenInclude(u => u.Profile)
            .Where(r => r.IsFlagged)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(MapReview).ToList();
    }

    public async Task<AdminReviewSummaryDto> SetReviewFlagStatusAsync(Guid reviewId, bool isFlagged)
    {
        var review = await _db.Reviews
            .Include(r => r.Reviewer).ThenInclude(u => u.Profile)
            .Include(r => r.Counselor!).ThenInclude(c => c.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(r => r.Id == reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        review.IsFlagged = isFlagged;
        await _db.SaveChangesAsync();
        return MapReview(review);
    }

    public async Task<IReadOnlyList<AdminTransactionSummaryDto>> GetTransactionsAsync()
    {
        var transactions = await _db.Transactions
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => new AdminTransactionSummaryDto(
            t.Id,
            t.UserId,
            t.User.Email,
            t.Type,
            t.Amount,
            t.Currency,
            t.PaymentMethod,
            t.Status,
            t.ReferenceId,
            t.ExternalTransactionId,
            t.CreatedAt,
            t.FailureReason)).ToList();
    }

    public async Task<AdminTransactionSummaryDto> RefundTransactionAsync(Guid transactionId, string reason)
    {
        var transaction = await _db.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException("Transaction not found.");

        if (transaction.Status != TransactionStatus.Completed)
            throw new InvalidOperationException("Only completed transactions can be refunded.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? "Refund processed by admin." : reason.Trim();
        transaction.Status = TransactionStatus.Refunded;
        transaction.FailureReason = cleanReason;

        if (transaction.Type == TransactionType.CounselingSession && transaction.ReferenceId.HasValue)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == transaction.ReferenceId.Value);
            if (session is not null && session.Status != SessionStatus.Completed)
            {
                session.Status = SessionStatus.Cancelled;
            }
        }

        await _db.SaveChangesAsync();

        return new AdminTransactionSummaryDto(
            transaction.Id,
            transaction.UserId,
            transaction.User.Email,
            transaction.Type,
            transaction.Amount,
            transaction.Currency,
            transaction.PaymentMethod,
            transaction.Status,
            transaction.ReferenceId,
            transaction.ExternalTransactionId,
            transaction.CreatedAt,
            transaction.FailureReason);
    }

    private static AdminReviewSummaryDto MapReview(Domain.Entities.Review review) => new(
        review.Id,
        review.ReviewerId,
        BuildName(review.Reviewer.Profile?.FirstName, review.Reviewer.Profile?.LastName),
        review.CounselorId,
        review.Counselor?.User?.Profile is null ? null : BuildName(review.Counselor.User.Profile.FirstName, review.Counselor.User.Profile.LastName),
        review.SessionId,
        review.Rating,
        review.ReviewText,
        review.IsAnonymous,
        review.IsFlagged,
        review.CreatedAt);

    private static string BuildName(string? firstName, string? lastName)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Unknown User" : name;
    }
}
