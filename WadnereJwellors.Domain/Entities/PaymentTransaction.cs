using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WadnereJwellors.Domain.Entities
{
    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? RegistrationId { get; set; }

        [Required]
        [StringLength(100)]
        public string OrderId { get; set; } = string.Empty;

        public string? CfOrderId { get; set; }

        public string? PaymentSessionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        [StringLength(50)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, USER_DROPPED

        public string? PaymentMode { get; set; }

        public string? ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
