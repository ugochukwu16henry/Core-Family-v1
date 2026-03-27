using CoreFamily.API.Domain.Enums;

namespace CoreFamily.API.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty; // stripe | paystack | google_pay
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? ExternalTransactionId { get; set; } // Stripe/Paystack ID
    public Guid? ReferenceId { get; set; } // Content/Program/Session ID
    public string? InvoiceUrl { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

public class Certificate : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ProgramId { get; set; }
    public string CertificateCode { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Program_? Program { get; set; }
}
