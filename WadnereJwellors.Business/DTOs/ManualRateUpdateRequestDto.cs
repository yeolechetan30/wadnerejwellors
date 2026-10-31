using System.ComponentModel.DataAnnotations;

namespace WadnereJwellors.Business.DTOs
{
    public class ManualRateUpdateRequestDto
    {
        [Range(1, 500000, ErrorMessage = "Gold 24K rate must be positive.")]
        public decimal Gold24K_PerGram { get; set; }

        [Range(1, 500000, ErrorMessage = "Gold 22K rate must be positive.")]
        public decimal Gold22K_PerGram { get; set; }

        [Range(1, 500000, ErrorMessage = "Gold 18K rate must be positive.")]
        public decimal Gold18K_PerGram { get; set; }

        [Range(1, 50000, ErrorMessage = "Silver rate per gram must be positive.")]
        public decimal Silver_PerGram { get; set; }
    }
}
