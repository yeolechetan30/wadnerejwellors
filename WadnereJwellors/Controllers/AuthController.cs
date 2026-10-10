using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.Business.Services;
using WadnereJwellors.Models;

namespace WadnereJwellors.Controllers
{
    [ApiController]
   // [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates user credentials, generates JWT & Refresh token, and sends 6-digit 2FA OTP via WhatsApp and registered Email.
        /// </summary>
        /// <param name="request">Mobile number and Password</param>
        [HttpPost("/api/Login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Verifies the 6-digit OTP sent to user's WhatsApp and registered Email for 2-Factor Authentication.
        /// </summary>
        /// <param name="request">Mobile number and 6-digit OTP code</param>
        [HttpPost("/api/Login/verify-otp")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            var response = await _authService.VerifyOtpAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Renews access token using a valid refresh token.
        /// </summary>
        /// <param name="request">Access Token and Refresh Token</param>
        [HttpPost("/api/Login/refresh-token")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Resends a 6-digit 2FA OTP code to user's WhatsApp mobile number and registered Email.
        /// </summary>
        /// <param name="mobileNumber">User registered mobile number</param>
        [HttpPost("/api/Login/resend-otp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendOtp([FromQuery] long mobileNumber)
        {
            await _authService.ResendOtpAsync(mobileNumber);
            return Ok(new { message = "6-digit 2FA OTP code has been resent to your WhatsApp mobile number and registered email address." });
        }

        /// <summary>
        /// Updates existing user password after verifying current password.
        /// </summary>
        /// <param name="request">Mobile number, current password, and new password</param>
        [HttpPost("/api/UpdatePassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDto request)
        {
            await _authService.UpdatePasswordAsync(request);
            return Ok(new { message = "Password updated successfully." });
        }

        /// <summary>
        /// Initiates forgot password process by generating and sending a 6-digit OTP code to user's WhatsApp mobile number and registered Email.
        /// </summary>
        /// <param name="request">Mobile number</param>
        [HttpPost("/api/ForgotPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "6-digit OTP code has been sent to your WhatsApp mobile number and registered email address for password reset." });
        }

        /// <summary>
        /// Resets user password after verifying the 6-digit OTP code sent via WhatsApp / Email.
        /// </summary>
        /// <param name="request">Mobile number, 6-digit OTP code, and new password</param>
        [HttpPost("/api/ResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Password reset successfully. You can now login with your new password." });
        }
    }
}
