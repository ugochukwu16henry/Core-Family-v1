using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
        var customerEmail = await GetUserEmailAsync(userId);
        var gatewayResult = await gateway.CreateCheckoutAsync(transaction, request, customerEmail);

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
        var customerEmail = await GetUserEmailAsync(userId);
        var gatewayResult = await gateway.CreateCheckoutAsync(transaction, request, customerEmail);

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

    public async Task HandleWebhookAsync(string provider, PaymentWebhookDto payload, string rawPayload, string? signature)
    {
        ValidateWebhookSignature(provider, rawPayload, signature);

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

        return transactions.Select(MapTransactionSummary).ToList();
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

    public async Task<TransactionSummaryDto> RequestRefundAsync(Guid userId, Guid transactionId, string reason)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId)
            ?? throw new KeyNotFoundException("Transaction not found.");

        if (transaction.Status != TransactionStatus.Completed)
            throw new InvalidOperationException("Only completed transactions can be submitted for refund or dispute review.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? "No reason provided." : reason.Trim();
        transaction.FailureReason = $"REFUND_REQUESTED: {cleanReason}";
        await _db.SaveChangesAsync();

        return MapTransactionSummary(transaction);
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

    private async Task<string> GetUserEmailAsync(Guid userId)
    {
        var email = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(email))
            throw new KeyNotFoundException("User email not found for payment checkout.");

        return email;
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

    private void ValidateWebhookSignature(string provider, string rawPayload, string? signature)
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

        var isValid = normalized switch
        {
            "stripe" => ValidateStripeSignature(rawPayload, signature, expected),
            "paystack" => ValidatePaystackSignature(rawPayload, signature, expected),
            "google_pay" => string.Equals(expected, signature, StringComparison.Ordinal),
            _ => false
        };

        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid webhook signature.");
        }
    }

    private static bool ValidateStripeSignature(string rawPayload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var parts = signatureHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var timestampPart = parts.FirstOrDefault(p => p.StartsWith("t=", StringComparison.Ordinal));
        var signaturePart = parts.FirstOrDefault(p => p.StartsWith("v1=", StringComparison.Ordinal));
        if (timestampPart is null || signaturePart is null)
            return false;

        var timestamp = timestampPart[2..];
        var providedSignature = signaturePart[3..];
        var signedPayload = $"{timestamp}.{rawPayload}";

        var computed = ComputeHmacHex(signedPayload, secret, useSha512: false);

        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTs))
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ageSeconds = Math.Abs(now - unixTs);
        if (ageSeconds > 300)
            return false;

        return FixedTimeEqualsHex(providedSignature, computed);
    }

    private static bool ValidatePaystackSignature(string rawPayload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var computed = ComputeHmacHex(rawPayload, secret, useSha512: true);
        return FixedTimeEqualsHex(signatureHeader, computed);
    }

    private static string ComputeHmacHex(string input, string secret, bool useSha512)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(input);

        byte[] hash = useSha512
            ? new HMACSHA512(keyBytes).ComputeHash(payloadBytes)
            : new HMACSHA256(keyBytes).ComputeHash(payloadBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEqualsHex(string left, string right)
    {
        if (left.Length != right.Length)
            return false;

        var leftBytes = Encoding.UTF8.GetBytes(left.ToLowerInvariant());
        var rightBytes = Encoding.UTF8.GetBytes(right.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
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

    internal static TransactionSummaryDto MapTransactionSummary(Transaction transaction) => new(
        transaction.Id,
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

public interface IPaymentGateway
{
    string Provider { get; }
    Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request, string customerEmail);
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
    private readonly IHttpClientFactory _httpClientFactory;

    public StripePaymentGateway(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public string Provider => "stripe";

    public async Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request, string customerEmail)
    {
        // Until Stripe SDK wiring is added, we support dev-safe auto completion when key is absent.
        var secretKey = _configuration["Stripe:SecretKey"];
        var checkoutBaseUrl = _configuration["Stripe:CheckoutBaseUrl"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new GatewayCheckoutResult($"mock_stripe_{Guid.NewGuid():N}", null, true, false);
        }

        var apiBaseUrl = _configuration["Stripe:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            apiBaseUrl = "https://api.stripe.com";

        var successUrl = request.SuccessUrl ?? _configuration["Stripe:DefaultSuccessUrl"] ?? "https://corefamily.com/payments/success";
        var cancelUrl = request.CancelUrl ?? _configuration["Stripe:DefaultCancelUrl"] ?? "https://corefamily.com/payments/cancel";
        var amountInMinorUnits = (int)Math.Round(transaction.Amount * 100m, MidpointRounding.AwayFromZero);

        var payload = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl,
            ["customer_email"] = customerEmail,
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = transaction.Currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = amountInMinorUnits.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"Core Family {transaction.Type}",
            ["metadata[transaction_id]"] = transaction.Id.ToString()
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        using var response = await client.PostAsync($"{apiBaseUrl.TrimEnd('/')}/v1/checkout/sessions", new FormUrlEncodedContent(payload));
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Stripe checkout init failed ({(int)response.StatusCode}).");

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        var checkoutUrl = root.TryGetProperty("url", out var urlEl)
            ? urlEl.GetString()
            : checkoutBaseUrl;

        var externalTransactionId = root.TryGetProperty("id", out var idEl)
            ? idEl.GetString()
            : $"stripe_{Guid.NewGuid():N}";

        var paymentStatus = root.TryGetProperty("payment_status", out var statusEl)
            ? statusEl.GetString()
            : null;

        var isCompleted = string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase);
        return new GatewayCheckoutResult(
            externalTransactionId ?? $"stripe_{Guid.NewGuid():N}",
            checkoutUrl,
            isCompleted,
            !isCompleted && !string.IsNullOrWhiteSpace(checkoutUrl));
    }
}

public class PaystackPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaystackPaymentGateway(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public string Provider => "paystack";

    public async Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request, string customerEmail)
    {
        var secretKey = _configuration["Paystack:SecretKey"];
        var checkoutBaseUrl = _configuration["Paystack:CheckoutBaseUrl"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new GatewayCheckoutResult($"mock_paystack_{Guid.NewGuid():N}", null, true, false);
        }

        var apiBaseUrl = _configuration["Paystack:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            apiBaseUrl = "https://api.paystack.co";

        var callbackUrl = request.SuccessUrl ?? _configuration["Paystack:DefaultCallbackUrl"] ?? "https://corefamily.com/payments/success";
        var amountInMinorUnits = (int)Math.Round(transaction.Amount * 100m, MidpointRounding.AwayFromZero);
        var reference = $"cf_{transaction.Id:N}";

        var payload = new
        {
            email = customerEmail,
            amount = amountInMinorUnits,
            currency = transaction.Currency,
            callback_url = callbackUrl,
            reference,
            metadata = new { transaction_id = transaction.Id.ToString() }
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        using var response = await client.PostAsJsonAsync($"{apiBaseUrl.TrimEnd('/')}/transaction/initialize", payload);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Paystack checkout init failed ({(int)response.StatusCode}).");

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;
        var data = root.TryGetProperty("data", out var dataEl) ? dataEl : default;

        var checkoutUrl = data.ValueKind != JsonValueKind.Undefined && data.TryGetProperty("authorization_url", out var authUrlEl)
            ? authUrlEl.GetString()
            : checkoutBaseUrl;

        var externalTransactionId = data.ValueKind != JsonValueKind.Undefined && data.TryGetProperty("reference", out var referenceEl)
            ? referenceEl.GetString()
            : reference;

        return new GatewayCheckoutResult(
            externalTransactionId ?? reference,
            checkoutUrl,
            false,
            !string.IsNullOrWhiteSpace(checkoutUrl));
    }
}

public class GooglePayPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    public GooglePayPaymentGateway(IConfiguration configuration) => _configuration = configuration;

    public string Provider => "google_pay";

    public Task<GatewayCheckoutResult> CreateCheckoutAsync(Transaction transaction, CreateCheckoutRequestDto request, string customerEmail)
    {
        // Google Pay is often processed through Stripe/Paystack rails; keep fallback implementation.
        var checkoutBaseUrl = _configuration["GooglePay:CheckoutBaseUrl"];
        var checkoutUrl = request.SuccessUrl
            ?? (string.IsNullOrWhiteSpace(checkoutBaseUrl)
                ? "https://pay.google.com/mock-session"
                : checkoutBaseUrl);
        return Task.FromResult(new GatewayCheckoutResult($"gpay_{Guid.NewGuid():N}", checkoutUrl, false, true));
    }
}
