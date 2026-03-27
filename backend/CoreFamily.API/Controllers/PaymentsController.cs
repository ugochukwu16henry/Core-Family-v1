using System.Security.Claims;
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

    [AllowAnonymous]
    [HttpPost("webhooks/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> HandleWebhook(string provider, [FromBody] PaymentWebhookDto payload)
    {
        var signature = Request.Headers["X-Payment-Signature"].ToString();
        await _payments.HandleWebhookAsync(provider, payload, signature);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(sub);
    }
}
