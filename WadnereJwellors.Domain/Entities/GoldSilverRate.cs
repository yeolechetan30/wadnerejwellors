using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WadnereJwellors.Domain.Entities
{
    [Table("GoldSilverRates")]
    public class GoldSilverRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Gold24K_PerGram { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Gold22K_PerGram { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Gold18K_PerGram { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Silver_PerGram { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Silver_PerKg { get; set; }

        [StringLength(50)]
        public string Source { get; set; } = "LiveMarket";

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
