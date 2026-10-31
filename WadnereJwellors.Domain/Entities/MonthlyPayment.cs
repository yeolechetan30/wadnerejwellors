using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WadnereJwellors.Domain.Entities
{
    [Table("MonthlyPayments")]
    public class MonthlyPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual UserRegistration? UserRegistration { get; set; }

        public int? PaymentTransactionId { get; set; }

        [ForeignKey("PaymentTransactionId")]
        public virtual PaymentTransaction? PaymentTransaction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public int PaymentMonth { get; set; }

        public int PaymentYear { get; set; }

        [Required]
        [StringLength(30)]
        public string MonthYearDisplay { get; set; } = string.Empty; // e.g. "October 2026"

        public bool IsRedeemed { get; set; } = false; // default 0

        public DateTime? RedeemedDate { get; set; }

        [StringLength(250)]
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
