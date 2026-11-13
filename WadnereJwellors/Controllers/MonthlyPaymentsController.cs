using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;

namespace WadnereJwellors.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MonthlyPaymentsController : ControllerBase
    {
        private readonly IMonthlyPaymentService _monthlyPaymentService;

        public MonthlyPaymentsController(IMonthlyPaymentService monthlyPaymentService)
        {
            _monthlyPaymentService = monthlyPaymentService;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] MonthlyPaymentRequestDto request)
        {
            if (request.RegistrationId <= 0)
            {
                return BadRequest(new { message = "Valid RegistrationId is required." });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero." });
            }

            var order = await _monthlyPaymentService.InitiateMonthlyPaymentOrderAsync(request);
            return Ok(order);
        }

        /// <summary>
        /// Records an offline monthly savings installment payment (cash/cheque).
        /// Adds entry directly to MonthlyPayments table with PaymentTransactionId = null.
        /// </summary>
        [HttpPost("offline-payment")]
        public async Task<IActionResult> RecordOfflinePayment([FromBody] OfflineMonthlyPaymentDto request)
        {
            if (request.RegistrationId <= 0)
            {
                return BadRequest(new { message = "Valid RegistrationId is required." });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than zero." });
            }

            try
            {
                var result = await _monthlyPaymentService.RecordOfflineMonthlyPaymentAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Gets the Passbook & financial ledger statement for a customer.
        /// Example: GET /api/monthlypayments/passbook/25
        /// </summary>
        [HttpGet("passbook/{registrationId}")]
        public async Task<IActionResult> GetPassbook(int registrationId)
        {
            if (registrationId <= 0)
            {
                return BadRequest(new { message = "Valid RegistrationId is required." });
            }

            var passbook = await _monthlyPaymentService.GetPassbookAsync(registrationId);
            return Ok(passbook);
        }

        /// <summary>
        /// Marks a specific month's payment as redeemed when customer claims it.
        /// Example: POST /api/monthlypayments/redeem/10
        /// </summary>
        [HttpPost("redeem/{paymentId}")]
        public async Task<IActionResult> RedeemPayment(int paymentId)
        {
            var success = await _monthlyPaymentService.RedeemMonthlyPaymentAsync(paymentId);
            if (!success)
            {
                return BadRequest(new { message = "Payment entry not found or is already redeemed." });
            }

            return Ok(new { success = true, message = "Monthly payment has been marked as redeemed successfully." });
        }
    }
}

