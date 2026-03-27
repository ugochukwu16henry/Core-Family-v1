using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreFamily.API.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly CoreFamilyDbContext _db;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly IConfiguration _configuration;

    public PaymentService(CoreFamilyDbContext db, IEnumerable<IPaymentGateway> gateways, IConfiguration configuration)
    {
        _db = db;
        _gateways = gateways;
        _configuration = configuration;
    }

    public async Task<CheckoutSessionDto> CreateProgramCheckoutAsync(Guid userId, Guid programId, CreateCheckoutRequestDto request)
    {
        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == programId && p.IsPublished)
            ?? throw new KeyNotFoundException("Program not found or unpublished.");

        var alreadyEnrolled = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.ProgramId == programId);
        if (alreadyEnrolled)
        {
            throw new InvalidOperationException("You are already enrolled in this program.");
        }

        var amount = program.Price;
        var transaction = await CreatePendingTransactionAsync(userId, TransactionType.ProgramEnrollment, amount, request, programId);

        if (amount <= 0)
        {
            transaction.Status = TransactionStatus.Completed;
            await _db.SaveChangesAsync();
            return MapCheckoutDto(transaction, requiresRedirect: false, checkoutUrl: null);
        }

        var gateway = ResolveGateway(request.Provider);
        var gatewayResult = await gateway.CreateCheckoutAsync(transaction, request);

        transaction.ExternalTransactionId = gatewayResult.ExternalTransactionId;
        transaction.InvoiceUrl = gatewayResult.CheckoutUrl;
        transaction.Status = gatewayResult.IsCompleted ? TransactionStatus.Completed : TransactionStatus.Pending;

        await _db.SaveChangesAsync();
        return MapCheckoutDto(transaction, gatewayResult.RequiresRedirect, gatewayResult.CheckoutUrl);
    }

    public async Task<CheckoutSessionDto> CreateSessionCheckoutAsync(Guid userId, Guid sessionId, CreateCheckoutRequestDto request)
    {
        var session = await _db.Sessions
            .Include(s => s.Counselor)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.ClientId != userId)
        {
            throw new UnauthorizedAccessException("You can only pay for your own session.");
        }

        var existingCompleted = await _db.Transactions.AnyAsync(t =>
            t.UserId == userId &&
            t.Type == TransactionType.CounselingSession &&
            t.ReferenceId == sessionId &&
            t.Status == TransactionStatus.Completed);

        if (existingCompleted)
        {
            throw new InvalidOperationException("This session is already paid.");
        }

        var transaction = await CreatePendingTransactionAsync(userId, TransactionType.CounselingSession, session.AmountPaid, request, sessionId);

        if (session.AmountPaid <= 0)
        {
            transaction.Status = TransactionStatus.Completed;
            session.Status = SessionStatus.Confirmed;
            await _db.SaveChangesAsync();
            return MapCheckoutDto(transaction, requiresRedirect: false, checkoutUrl: null);
        }

        var gateway = ResolveGateway(request.Provider);
        var gatewayResult = await gateway.CreateCheckoutAsync(transaction, request);

        transaction.ExternalTransactionId = gatewayResult.ExternalTransactionId;
        transaction.InvoiceUrl = gatewayResult.CheckoutUrl;
        transaction.Status = gatewayResult.IsCompleted ? TransactionStatus.Completed : TransactionStatus.Pending;

        if (transaction.Status == TransactionStatus.Completed)
        {
            session.Status = SessionStatus.Confirmed;
        }

        await _db.SaveChangesAsync();
        return MapCheckoutDto(transaction, gatewayResult.RequiresRedirect, gatewayResult.CheckoutUrl);
    }

    public async Task HandleWebhookAsync(string provider, PaymentWebhookDto payload, string? signature)
    {
        ValidateWebhookSignature(provider, signature);

        var normalizedProvider = NormalizeProvider(provider);
        var transaction = await _db.Transactions.FirstOrDefaultAsync(t =>
            t.PaymentMethod == normalizedProvider &&
            t.ExternalTransactionId == payload.ExternalTransactionId);

        if (transaction is null)
        {
            throw new KeyNotFoundException("Transaction not found for webhook payload.");
        }

        var status = payload.Status.Trim().ToLowerInvariant();
        transaction.InvoiceUrl = payload.InvoiceUrl ?? transaction.InvoiceUrl;

        switch (status)
        {
            case "completed":
            case "success":
            case "paid":
                transaction.Status = TransactionStatus.Completed;
                transaction.FailureReason = null;
                break;
            case "failed":
            case "error":
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = payload.FailureReason ?? "Payment failed.";
                break;
            case "refunded":
                transaction.Status = TransactionStatus.Refunded;
                transaction.FailureReason = payload.FailureReason;
                break;
            default:
                throw new ArgumentException("Unsupported webhook status.");
        }

        if (transaction.Type == TransactionType.CounselingSession && transaction.ReferenceId.HasValue)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == transaction.ReferenceId.Value);
            if (session is not null)
            {
                if (transaction.Status == TransactionStatus.Completed)
                {
                    session.Status = SessionStatus.Confirmed;
                }
                else if (transaction.Status == TransactionStatus.Refunded && session.Status != SessionStatus.Completed)
                {
                    session.Status = SessionStatus.Cancelled;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TransactionSummaryDto>> GetMyTransactionsAsync(Guid userId)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => new TransactionSummaryDto(
            t.Id,
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

    public async Task<bool> HasCompletedProgramPaymentAsync(Guid userId, Guid programId)
    {
        return await _db.Transactions.AnyAsync(t =>
            t.UserId == userId &&
            t.Type == TransactionType.ProgramEnrollment &&
            t.ReferenceId == programId &&
            t.Status == TransactionStatus.Completed);
    }

    public async Task<bool> HasCompletedSessionPaymentAsync(Guid userId, Guid sessionId)
    {
        return await _db.Transactions.AnyAsync(t =>
            t.UserId == userId &&
            t.Type == TransactionType.CounselingSession &&
            t.ReferenceId == sessionId &&
            t.Status == TransactionStatus.Completed);
    }

    private async Task<Transaction> CreatePendingTransactionAsync(
        Guid userId,
        TransactionType type,
        decimal amount,
        CreateCheckoutRequestDto request,
        Guid referenceId)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            Type = type,
            Amount = amount,
            Currency = NormalizeCurrency(request.Currency),
            PaymentMethod = NormalizeProvider(request.Provider),
            Status = TransactionStatus.Pending,
            ReferenceId = referenceId
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    private IPaymentGateway ResolveGateway(string provider)
    {
        var normalized = NormalizeProvider(provider);
        var gateway = _gateways.FirstOrDefault(g => g.Provider == normalized);
        if (gateway is null)
        {
            throw new ArgumentException($"Unsupported payment provider '{provider}'.");
        }

        return gateway;
    }

    private void ValidateWebhookSignature(string provider, string? signature)
    {
        var normalized = NormalizeProvider(provider);
        var expected = normalized switch
        {
            "stripe" => _configuration["Stripe:WebhookSecret"],
            "paystack" => _configuration["Paystack:WebhookSecret"],
            "google_pay" => _configuration["GooglePay:WebhookSecret"],
            _ => null
        };

        if (string.IsNullOrWhiteSpace(expected))
        {
            // Allow dev environments without configured webhook secrets.
            return;
        }

        if (!string.Equals(expected, signature, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid webhook signature.");
        }
    }

    private static string NormalizeProvider(string provider)
    {
        var normalized = provider.Trim().ToLowerInvariant();
        if (normalized is "googlepay" or "google-pay")
        {
            normalized = "google_pay";
        }

        return normalized;
    }

    private static string NormalizeCurrency(string currency)
    {
        var value = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        return value.Length > 3 ? value[..3] : value;
    }

    private static CheckoutSessionDto MapCheckoutDto(Transaction transaction, bool requiresRedirect, string? checkoutUrl) => new(
        transaction.Id,
        transaction.Type,
        transaction.Status,
        transaction.Amount,
        transaction.Currency,
        transaction.PaymentMethod,
        checkoutUrl,
        transaction.ExternalTransactionId,
        requiresRedirect);
}

public interface IPaymentGateway
{
    string Provider { get; }
    Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request);
}

public record GatewayCheckoutResult(
    string ExternalTransactionId,
    string? CheckoutUrl,
    bool IsCompleted,
    bool RequiresRedirect
);

public class StripePaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    public StripePaymentGateway(IConfiguration configuration) => _configuration = configuration;

    public string Provider => "stripe";

    public Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request)
    {
        // Until Stripe SDK wiring is added, we support dev-safe auto completion when key is absent.
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Task.FromResult(new GatewayCheckoutResult($"mock_stripe_{Guid.NewGuid():N}", null, true, false));
        }

        var checkoutUrl = request.SuccessUrl ?? "https://checkout.stripe.com/pay/mock-session";
        return Task.FromResult(new GatewayCheckoutResult($"stripe_{Guid.NewGuid():N}", checkoutUrl, false, true));
    }
}

public class PaystackPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    public PaystackPaymentGateway(IConfiguration configuration) => _configuration = configuration;

    public string Provider => "paystack";

    public Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request)
    {
        var secretKey = _configuration["Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Task.FromResult(new GatewayCheckoutResult($"mock_paystack_{Guid.NewGuid():N}", null, true, false));
        }

        var checkoutUrl = request.SuccessUrl ?? "https://checkout.paystack.com/mock-session";
        return Task.FromResult(new GatewayCheckoutResult($"paystack_{Guid.NewGuid():N}", checkoutUrl, false, true));
    }
}

public class GooglePayPaymentGateway : IPaymentGateway
{
    public string Provider => "google_pay";

    public Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request)
    {
        // Google Pay is often processed through Stripe/Paystack rails; keep fallback implementation.
        var checkoutUrl = request.SuccessUrl ?? "https://pay.google.com/mock-session";
        return Task.FromResult(new GatewayCheckoutResult($"gpay_{Guid.NewGuid():N}", checkoutUrl, false, true));
    }
}
