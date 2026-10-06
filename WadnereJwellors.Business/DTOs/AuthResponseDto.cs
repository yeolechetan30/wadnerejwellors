using System;

namespace WadnereJwellors.Business.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? TokenExpiresAt { get; set; }
        public bool RequiresOtpVerification { get; set; }
        public int RegistrationId { get; set; }
        public long MobileNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
