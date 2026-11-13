using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;
using WadnereJwellors.Models;

namespace WadnereJwellors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ICashfreePaymentService _paymentService;

        public PaymentsController(ICashfreePaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Creates a Cashfree payment order and returns a payment_session_id.
        /// Requires valid JWT Bearer Token.
        /// </summary>
        [Authorize]
        [HttpPost("create-order")]
        [ProducesResponseType(typeof(PaymentOrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentOrderResponseDto>> CreateOrder([FromBody] CreatePaymentRequestDto request)
        {
            // Auto-populate customer claims from JWT Bearer Token if not explicitly provided
            if (!request.RegistrationId.HasValue && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value, out var regId))
            {
                request.RegistrationId = regId;
            }

            if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                request.CustomerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                request.CustomerName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                request.CustomerPhone = User.FindFirst("MobileNumber")?.Value ?? string.Empty;
            }

            var response = await _paymentService.CreateOrderAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves the current payment status for an order and synchronizes with Cashfree.
        /// Can be called with or without authentication (e.g. from payment gateway return/callback URL).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("status/{orderId}")]
        [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentStatusDto>> GetStatus(string orderId)
        {
            var status = await _paymentService.GetOrderStatusAsync(orderId);
            return Ok(status);
        }

        /// <summary>
        /// Handles browser/app redirect from Cashfree after payment completion.
        /// Synchronizes order status in database and renders a responsive confirmation receipt page.
        /// Routes supported:
        ///   GET /payment/callback?order_id={order_id}
        ///   GET /api/Payments/callback?order_id={order_id}
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/payment/callback")]
        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallback([FromQuery(Name = "order_id")] string? orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Content("<html><body><h3>Error: Missing order_id parameter in callback.</h3></body></html>", "text/html");
            }

            PaymentStatusDto status;
            try
            {
                status = await _paymentService.GetOrderStatusAsync(orderId);
            }
            catch (Exception ex)
            {
                return Content($"<html><body style='font-family:sans-serif;padding:30px;'><h3>Error verifying order {orderId}:</h3><p>{ex.Message}</p></body></html>", "text/html");
            }

            // Return JSON if caller specifically requests application/json
            if (Request.Headers.Accept.ToString().Contains("application/json") && !Request.Headers.Accept.ToString().Contains("text/html"))
            {
                return Ok(status);
            }

            bool isPaid = status.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
                          status.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);

            string statusColor = isPaid ? "#16a34a" : "#dc2626";
            string statusIcon = isPaid ? "&#10004;" : "&#10008;";
            string statusTitle = isPaid ? "Payment Successful!" : "Payment " + status.Status;
            string message = isPaid
                ? "Your transaction has been verified successfully. Your membership / savings installment has been recorded."
                : "Your payment was not completed or was cancelled. Please try again.";

            string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Wadnere Jewellers - Payment Receipt</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background-color: #f8fafc;
            margin: 0;
            padding: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            box-sizing: border-box;
        }}
        .card {{
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
            max-width: 440px;
            width: 100%;
            padding: 32px 24px;
            text-align: center;
            border: 1px solid #e2e8f0;
        }}
        .icon-circle {{
            width: 64px;
            height: 64px;
            background-color: {(isPaid ? "#dcfce7" : "#fee2e2")};
            color: {statusColor};
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 32px;
            margin: 0 auto 16px auto;
        }}
        h2 {{
            color: #0f172a;
            margin: 0 0 8px 0;
            font-size: 22px;
        }}
        p.subtitle {{
            color: #64748b;
            font-size: 14px;
            margin: 0 0 24px 0;
            line-height: 1.5;
        }}
        .details-box {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 16px;
            margin-bottom: 24px;
            text-align: left;
            font-size: 13px;
        }}
        .row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #edf2f7;
        }}
        .row:last-child {{
            border-bottom: none;
        }}
        .label {{
            color: #64748b;
        }}
        .value {{
            font-weight: 600;
            color: #0f172a;
            word-break: break-all;
        }}
        .brand {{
            font-size: 11px;
            color: #94a3b8;
            margin-top: 16px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""icon-circle"">{statusIcon}</div>
        <h2>{statusTitle}</h2>
        <p class=""subtitle"">{message}</p>

        <div class=""details-box"">
            <div class=""row"">
                <span class=""label"">Amount</span>
                <span class=""value"">&#8377;{status.Amount:N2}</span>
            </div>
            <div class=""row"">
                <span class=""label"">Order ID</span>
                <span class=""value"">{status.OrderId}</span>
            </div>
            <div class=""row"">
                <span class=""label"">Payment Mode</span>
                <span class=""value"">{(string.IsNullOrWhiteSpace(status.PaymentMode) ? "UPI / Online" : status.PaymentMode.ToUpper())}</span>
            </div>
            <div class=""row"">
                <span class=""label"">Status</span>
                <span class=""value"" style=""color: {statusColor};"">{status.Status}</span>
            </div>
        </div>

        <div class=""brand"">Wadnere Jewellers &bull; Purity & Trust</div>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }

        /// <summary>
        /// Cashfree Server-to-Server Webhook handler.
        /// Note: Must remain [AllowAnonymous] because Cashfree calls this directly without a user JWT token.
        /// Authenticity is verified using Cashfree's HMAC-SHA256 signature in the request headers.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["x-webhook-signature"].ToString();
            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();

            if (!_paymentService.VerifyWebhookSignature(rawBody, signature, timestamp))
            {
                return Unauthorized(new { message = "Invalid webhook signature" });
            }

            await _paymentService.ProcessWebhookEventAsync(rawBody);
            return Ok(new { message = "Webhook processed" });
        }
    }
}
