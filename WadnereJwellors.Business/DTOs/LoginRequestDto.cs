using System.ComponentModel.DataAnnotations;

namespace WadnereJwellors.Business.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Mobile number is required.")]
        public long MobileNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}
