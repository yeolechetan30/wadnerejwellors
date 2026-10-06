using System.ComponentModel.DataAnnotations;

namespace WadnereJwellors.Business.DTOs
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Mobile number is required.")]
        public long MobileNumber { get; set; }

        [Required(ErrorMessage = "6-digit OTP code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits.")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and Confirm password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
