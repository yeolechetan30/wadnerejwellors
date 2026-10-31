using System;

namespace WadnereJwellors.Business.DTOs
{
    public class GoldSilverRateDto
    {
        public decimal Gold24K_PerGram { get; set; }
        public decimal Gold22K_PerGram { get; set; }
        public decimal Gold18K_PerGram { get; set; }
        public decimal Gold24K_Per10Gram => Gold24K_PerGram * 10;
        public decimal Gold22K_Per10Gram => Gold22K_PerGram * 10;
        public decimal Gold18K_Per10Gram => Gold18K_PerGram * 10;
        
        public decimal Silver_PerGram { get; set; }
        public decimal Silver_PerKg { get; set; }

        public string Source { get; set; } = "LiveMarket";
        public string Currency { get; set; } = "INR";
        public DateTime Timestamp { get; set; }
    }
}
