using Xunit;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using CoreFamily.API.Domain.Entities;
using CoreFamily.API.Domain.Enums;
using CoreFamily.API.Infrastructure.Data;
using CoreFamily.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CoreFamily.Tests.Integration;

/// <summary>
/// Integration tests for payment webhook handling, signature validation, and transaction state transitions.
/// Tests cover Stripe  (SHA256 with replay window), Paystack (SHA512), and provider-aware event mapping.
/// </summary>
public class PaymentWebhookIntegrationTests
{
    private readonly CoreFamilyDbContext _dbContext;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly PaymentService _paymentService;
    private readonly User _testUser;

    public PaymentWebhookIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CoreFamilyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CoreFamilyDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Setup configuration mocks
        _configurationMock = new Mock<IConfiguration>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Create test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();

        // Create payment service with all gateway implementations
        var gateways = new List<IPaymentGateway>
        {
            new StripePaymentGateway(_configurationMock.Object, _httpClientFactoryMock.Object),
            new PaystackPaymentGateway(_configurationMock.Object, _httpClientFactoryMock.Object),
            new GooglePayPaymentGateway(_configurationMock.Object)
        };

        _paymentService = new PaymentService(_dbContext, gateways, _configurationMock.Object);
    }

    #region Stripe Webhook Signature Tests

    [Fact]
    public async Task HandleWebhook_ValidStripeSignature_UpdatesTransactionSuccessfully()
    {
        // Arrange
        _configurationMock
            .Setup(c => c["Stripe:WebhookSecret"])
            .Returns("whsec_test_stripe_secret_key_12345678901234567890");

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_stripe_001");

        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "whsec_test_stripe_secret_key_12345678901234567890");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "completed",
            null,
            null
        ), rawPayload, signature);

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(updatedTransaction);
        Assert.Equal(TransactionStatus.Completed, updatedTransaction.Status);
        Assert.Null(updatedTransaction.FailureReason);
    }

    [Fact]
    public async Task HandleWebhook_InvalidStripeSignature_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _configurationMock
            .Setup(c => c["Stripe:WebhookSecret"])
            .Returns("whsec_test_stripe_secret_key_12345678901234567890");

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_stripe_002");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var invalidSignature = "t=1234567890,v1=invalidsignatureabcdef1234567890";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
                transaction.ExternalTransactionId!,
                "completed",
                null,
                null
            ), rawPayload, invalidSignature));
    }

    [Fact]
    public async Task HandleWebhook_StripeReplayAttack_ThrowsUnauthorizedAccessException()
    {
        // Arrange - Create a signature with timestamp older than 300 seconds (replay window)
        _configurationMock
            .Setup(c => c["Stripe:WebhookSecret"])
            .Returns("whsec_test_stripe_secret_key_12345678901234567890");

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_stripe_003");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);

        // Timestamp from 400 seconds ago (beyond 300s replay window)
        var oldTimestamp = DateTimeOffset.UtcNow.AddSeconds(-400).ToUnixTimeSeconds().ToString();
        var signature = GenerateStripeSignatureWithTimestamp(
            rawPayload,
            "whsec_test_stripe_secret_key_12345678901234567890",
            oldTimestamp
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
                transaction.ExternalTransactionId!,
                "completed",
                null,
                null
            ), rawPayload, signature));
    }

    #endregion

    #region Paystack Webhook Signature Tests

    [Fact]
    public async Task HandleWebhook_ValidPaystackSignature_UpdatesTransactionSuccessfully()
    {
        // Arrange
        _configurationMock
            .Setup(c => c["Paystack:WebhookSecret"])
            .Returns("paystack_test_secret_key_12345678901234567890");

        var transaction = CreateTransaction(_testUser.Id, "paystack", "cf_paystack_ref_001");

        var payload = new
        {
            @event = "charge.success",
            data = new
            {
                reference = transaction.ExternalTransactionId,
                status = "success"
            }
        };

        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GeneratePaystackSignature(rawPayload, "paystack_test_secret_key_12345678901234567890");

        // Act
        await _paymentService.HandleWebhookAsync("paystack", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "success",
            null,
            null
        ), rawPayload, signature);

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(updatedTransaction);
        Assert.Equal(TransactionStatus.Completed, updatedTransaction.Status);
    }

    [Fact]
    public async Task HandleWebhook_InvalidPaystackSignature_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _configurationMock
            .Setup(c => c["Paystack:WebhookSecret"])
            .Returns("paystack_test_secret_key_12345678901234567890");

        var transaction = CreateTransaction(_testUser.Id, "paystack", "cf_paystack_ref_002");
        var payload = new { @event = "charge.success" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var invalidSignature = "invalidsignatureabcdef1234567890abcdef1234567890";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _paymentService.HandleWebhookAsync("paystack", new PaymentWebhookDto(
                transaction.ExternalTransactionId!,
                "success",
                null,
                null
            ), rawPayload, invalidSignature));
    }

    #endregion

    #region Transaction Status Transition Tests

    [Theory]
    [InlineData("completed", TransactionStatus.Completed)]
    [InlineData("success", TransactionStatus.Completed)]
    [InlineData("paid", TransactionStatus.Completed)]
    public async Task HandleWebhook_CompletedStatus_UpdatesTransactionToCompleted(
        string webhookStatus,
        TransactionStatus expectedStatus)
    {
        // Arrange
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_004");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            webhookStatus,
            null,
            null
        ), rawPayload, signature);

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.Equal(expectedStatus, updatedTransaction!.Status);
        Assert.Null(updatedTransaction.FailureReason);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("error")]
    public async Task HandleWebhook_FailedStatus_UpdatesTransactionToFailed(string webhookStatus)
    {
        // Arrange
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_005");
        var payload = new { type = "charge.failed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            webhookStatus,
            "Card declined",
            null
        ), rawPayload, signature);

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.Equal(TransactionStatus.Failed, updatedTransaction!.Status);
        Assert.Equal("Card declined", updatedTransaction.FailureReason);
    }

    [Fact]
    public async Task HandleWebhook_RefundedStatus_UpdatesTransactionToRefunded()
    {
        // Arrange
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_006");
        transaction.Status = TransactionStatus.Completed; // Must be completed first
        _dbContext.SaveChanges();

        var payload = new { type = "charge.refunded" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "refunded",
            "Customer requested refund",
            null
        ), rawPayload, signature);

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.Equal(TransactionStatus.Refunded, updatedTransaction!.Status);
        Assert.Equal("Customer requested refund", updatedTransaction.FailureReason);
    }

    #endregion

    #region Session Status Update Tests

    [Fact]
    public async Task HandleWebhook_CompletedPayment_UpdatesSessionStatusToConfirmed()
    {
        // Arrange
        var counselorUser = new User { Id = Guid.NewGuid(), Email = "counselor@test.com", CreatedAt = DateTime.UtcNow };
        _dbContext.Users.Add(counselorUser);

        var counselor = new CounselorProfile
        {
            Id = Guid.NewGuid(),
            UserId = counselorUser.Id,
            LicenseStatus = VerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CounselorProfiles.Add(counselor);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ClientId = _testUser.Id,
            CounselorId = counselor.Id,
            AmountPaid = 100m,
            Status = SessionStatus.Pending,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Sessions.Add(session);

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_session_001");
        transaction.Type = TransactionType.CounselingSession;
        transaction.ReferenceId = session.Id;
        _dbContext.SaveChanges();

        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "completed",
            null,
            null
        ), rawPayload, signature);

        // Assert
        var updatedSession = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(SessionStatus.Confirmed, updatedSession.Status);
    }

    [Fact]
    public async Task HandleWebhook_RefundedPayment_UpdatesSessionStatusToCancelledIfNotCompleted()
    {
        // Arrange
        var counselorUser = new User { Id = Guid.NewGuid(), Email = "counselor2@test.com", CreatedAt = DateTime.UtcNow };
        _dbContext.Users.Add(counselorUser);

        var counselor = new CounselorProfile
        {
            Id = Guid.NewGuid(),
            UserId = counselorUser.Id,
            LicenseStatus = VerificationStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CounselorProfiles.Add(counselor);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ClientId = _testUser.Id,
            CounselorId = counselor.Id,
            AmountPaid = 100m,
            Status = SessionStatus.Pending,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Sessions.Add(session);

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_session_002");
        transaction.Type = TransactionType.CounselingSession;
        transaction.ReferenceId = session.Id;
        transaction.Status = TransactionStatus.Completed;
        _dbContext.SaveChanges();

        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var payload = new { type = "charge.refunded" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "refunded",
            "Customer requested refund",
            null
        ), rawPayload, signature);

        // Assert
        var updatedSession = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(SessionStatus.Cancelled, updatedSession.Status);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task HandleWebhook_TransactionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("secret");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);
        var signature = GenerateStripeSignature(rawPayload, "secret");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
                "ch_nonexistent_999",
                "completed",
                null,
                null
            ), rawPayload, signature));
    }

    [Fact]
    public async Task HandleWebhook_NoWebhookSecretConfigured_AllowsWebhook()
    {
        // Arrange - No webhook secret configured (allows in dev environments)
        _configurationMock.Setup(c => c["Stripe:WebhookSecret"]).Returns((string?)null);

        var transaction = CreateTransaction(_testUser.Id, "stripe", "ch_test_007");
        var payload = new { type = "checkout.session.completed" };
        var rawPayload = JsonSerializer.Serialize(payload);

        // Act - Should not throw even though signature is invalid/empty
        await _paymentService.HandleWebhookAsync("stripe", new PaymentWebhookDto(
            transaction.ExternalTransactionId!,
            "completed",
            null,
            null
        ), rawPayload, "");

        // Assert
        var updatedTransaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);
        Assert.Equal(TransactionStatus.Completed, updatedTransaction!.Status);
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTransaction(
        Guid userId,
        string paymentMethod,
        string externalTransactionId)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = TransactionType.CounselingSession,
            Amount = 100m,
            Currency = "USD",
            PaymentMethod = paymentMethod,
            Status = TransactionStatus.Pending,
            ExternalTransactionId = externalTransactionId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        _dbContext.SaveChanges();
        return transaction;
    }

    private static string GenerateStripeSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        return GenerateStripeSignatureWithTimestamp(payload, secret, timestamp);
    }

    private static string GenerateStripeSignatureWithTimestamp(string payload, string secret, string timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);
        var hash = new HMACSHA256(keyBytes).ComputeHash(payloadBytes);
        var hexHash = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={hexHash}";
    }

    private static string GeneratePaystackSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = new HMACSHA512(keyBytes).ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}
