using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Application.DTOs;

public record CreateCheckoutRequestDto(
    string Provider,
    string Currency = "USD",
    string? SuccessUrl = null,
    string? CancelUrl = null
);

public record CheckoutSessionDto(
    Guid TransactionId,
    TransactionType Type,
    TransactionStatus Status,
    decimal Amount,
    string Currency,
    string Provider,
    string? CheckoutUrl,
    string? ExternalTransactionId,
    bool RequiresRedirect
);

public record TransactionSummaryDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    TransactionStatus Status,
    Guid? ReferenceId,
    string? ExternalTransactionId,
    DateTime CreatedAt,
    string? FailureReason
);

public record PaymentWebhookDto(
    string ExternalTransactionId,
    string Status,
    string? FailureReason = null,
    string? InvoiceUrl = null
);
