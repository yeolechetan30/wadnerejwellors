using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;

namespace WadnereJwellors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ICashfreePaymentService _paymentService;

        public PaymentsController(ICashfreePaymentService paymentService) {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Creates a Cashfree payment order and returns a payment_session_id.
        /// </summary>
        [HttpPost("create-order")]
        public async Task<ActionResult<PaymentOrderResponseDto>> CreateOrder([FromBody] CreatePaymentRequestDto request) {
            var response = await _paymentService.CreateOrderAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves the current payment status for an order.
        /// </summary>
        [HttpGet("status/{orderId}")]
        public async Task<ActionResult<PaymentStatusDto>> GetStatus(string orderId) {
            var status = await _paymentService.GetOrderStatusAsync(orderId);
            return Ok(status);
        }

        /// <summary>
        /// Cashfree Server-to-Server Webhook handler.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook() {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["x-webhook-signature"].ToString();
            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();

            if (!_paymentService.VerifyWebhookSignature(rawBody, signature, timestamp)) {
                return Unauthorized(new { message = "Invalid webhook signature" });
            }

            await _paymentService.ProcessWebhookEventAsync(rawBody);
            return Ok(new { message = "Webhook processed" });
        }
    }
}
