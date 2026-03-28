using System.Security.Claims;
using System.Text.Json;
using CoreFamily.API.Application.DTOs;
using CoreFamily.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFamily.API.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    [HttpPost("checkout/program/{programId:guid}")]
    [ProducesResponseType(typeof(CheckoutSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckoutProgram(Guid programId, [FromBody] CreateCheckoutRequestDto request)
        => Ok(await _payments.CreateProgramCheckoutAsync(GetCurrentUserId(), programId, request));

    [HttpPost("checkout/session/{sessionId:guid}")]
    [ProducesResponseType(typeof(CheckoutSessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckoutSession(Guid sessionId, [FromBody] CreateCheckoutRequestDto request)
        => Ok(await _payments.CreateSessionCheckoutAsync(GetCurrentUserId(), sessionId, request));

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactions()
        => Ok(await _payments.GetMyTransactionsAsync(GetCurrentUserId()));

    [HttpPost("{transactionId:guid}/refund-request")]
    [ProducesResponseType(typeof(TransactionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRefund(Guid transactionId, [FromBody] RequestRefundDto dto)
        => Ok(await _payments.RequestRefundAsync(GetCurrentUserId(), transactionId, dto.Reason));

    [AllowAnonymous]
    [HttpPost("webhooks/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> HandleWebhook(string provider)
    {
        var normalizedProvider = NormalizeProvider(provider);
        var signature = normalizedProvider switch
        {
            "stripe" => Request.Headers["Stripe-Signature"].ToString(),
            "paystack" => Request.Headers["x-paystack-signature"].ToString(),
            _ => Request.Headers["X-Payment-Signature"].ToString()
        };

        if (string.IsNullOrWhiteSpace(signature))
        {
            signature = Request.Headers["X-Payment-Signature"].ToString();
        }

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var payload = ParseWebhookPayload(normalizedProvider, rawPayload);
        await _payments.HandleWebhookAsync(normalizedProvider, payload, rawPayload, signature);
        return NoContent();
    }

    private static PaymentWebhookDto ParseWebhookPayload(string provider, string rawPayload)
    {
        using var json = JsonDocument.Parse(rawPayload);
        var root = json.RootElement;

        return provider switch
        {
            "stripe" => ParseStripePayload(root),
            "paystack" => ParsePaystackPayload(root),
            _ => ParseDefaultPayload(rawPayload)
        };
    }

    private static PaymentWebhookDto ParseStripePayload(JsonElement root)
    {
        var type = root.TryGetProperty("type", out var typeElement)
            ? (typeElement.GetString() ?? string.Empty)
            : string.Empty;

        var payloadObject = root.TryGetProperty("data", out var dataElement)
            && dataElement.TryGetProperty("object", out var objectElement)
            ? objectElement
            : root;

        var externalId = payloadObject.TryGetProperty("id", out var idElement)
            ? (idElement.GetString() ?? string.Empty)
            : string.Empty;

        var paymentStatus = payloadObject.TryGetProperty("payment_status", out var paymentStatusElement)
            ? (paymentStatusElement.GetString() ?? string.Empty)
            : string.Empty;

        var status = type switch
        {
            "checkout.session.completed" => "completed",
            "checkout.session.async_payment_failed" => "failed",
            "charge.refunded" => "refunded",
            _ => paymentStatus switch
            {
                "paid" => "completed",
                "unpaid" => "failed",
                _ => "failed"
            }
        };

        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("Stripe webhook payload missing session identifier.");

        var invoiceUrl = payloadObject.TryGetProperty("url", out var urlElement)
            ? urlElement.GetString()
            : null;

        return new PaymentWebhookDto(externalId, status, null, invoiceUrl);
    }

    private static PaymentWebhookDto ParsePaystackPayload(JsonElement root)
    {
        var eventType = root.TryGetProperty("event", out var eventElement)
            ? (eventElement.GetString() ?? string.Empty)
            : string.Empty;

        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : root;

        var externalId = data.TryGetProperty("reference", out var referenceElement)
            ? (referenceElement.GetString() ?? string.Empty)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("Paystack webhook payload missing reference.");

        var dataStatus = data.TryGetProperty("status", out var statusElement)
            ? (statusElement.GetString() ?? string.Empty)
            : string.Empty;

        var normalizedStatus = eventType switch
        {
            "charge.success" => "success",
            "charge.failed" => "failed",
            "refund.processed" => "refunded",
            _ => dataStatus switch
            {
                "success" => "success",
                "failed" => "failed",
                "refunded" => "refunded",
                _ => "failed"
            }
        };

        var failureReason = data.TryGetProperty("gateway_response", out var responseElement)
            ? responseElement.GetString()
            : null;

        return new PaymentWebhookDto(externalId, normalizedStatus, failureReason, null);
    }

    private static PaymentWebhookDto ParseDefaultPayload(string rawPayload)
    {
        try
        {
            return JsonSerializer.Deserialize<PaymentWebhookDto>(rawPayload,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new ArgumentException("Webhook body is required.");
        }
        catch (JsonException)
        {
            throw new ArgumentException("Invalid webhook payload format.");
        }
    }

    private static string NormalizeProvider(string provider)
    {
        var normalized = provider.Trim().ToLowerInvariant();
        return normalized switch
        {
            "googlepay" or "google-pay" => "google_pay",
            _ => normalized
        };
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}
