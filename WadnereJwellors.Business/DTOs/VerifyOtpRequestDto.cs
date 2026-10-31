using System.ComponentModel.DataAnnotations;

namespace WadnereJwellors.Business.DTOs
{
    public class VerifyOtpRequestDto
    {
        [Required(ErrorMessage = "Mobile number is required.")]
        public long MobileNumber { get; set; }

        [Required(ErrorMessage = "6-digit OTP code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
