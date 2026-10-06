using System.ComponentModel.DataAnnotations;

namespace WadnereJwellors.Business.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Mobile number is required.")]
        public long MobileNumber { get; set; }
    }
}
