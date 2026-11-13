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
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;

        public MembershipController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        [HttpGet("status/{registrationId}")]
        public async Task<IActionResult> GetStatus(int registrationId)
        {
            var status = await _membershipService.CheckMembershipStatusAsync(registrationId);
            return Ok(status);
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateMembershipOrder([FromBody] MembershipRequestDto request)
        {
            if (request.RegistrationId <= 0)
            {
                return BadRequest(new { message = "Valid RegistrationId is required." });
            }

            var order = await _membershipService.InitiateMembershipOrderAsync(request);
            return Ok(order);
        }

        /// <summary>
        /// Records an offline membership payment (cash/cheque). 
        /// Adds entry directly to UserMemberships with PaymentTransactionId = null.
        /// Route: POST /api/Membership/offline-payment or POST /api/Membership/offline
        /// </summary>
        [AllowAnonymous]
        [HttpPost("offline-payment")]
        [HttpPost("offline")]
        [ProducesResponseType(typeof(OfflineMembershipResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOfflineMembership([FromBody] OfflineMembershipPaymentDto request)
        {
            if (request.RegistrationId <= 0)
            {
                return BadRequest(new { message = "Valid RegistrationId is required." });
            }

            try
            {
                var result = await _membershipService.CreateOfflineMembershipAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
