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
        /// Retrieves the current payment status for an order.
        /// Requires valid JWT Bearer Token.
        /// </summary>
        [Authorize]
        [HttpGet("status/{orderId}")]
        [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentStatusDto>> GetStatus(string orderId)
        {
            var status = await _paymentService.GetOrderStatusAsync(orderId);
            return Ok(status);
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
