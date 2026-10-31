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
        public string Status { get; set; } = "PENDING"; // PENDING, PAID, SUCCESS, FAILED, USER_DROPPED

        [StringLength(100)]
        public string? CustomerName { get; set; }

        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        [StringLength(255)]
        public string? OrderNote { get; set; }

        [StringLength(50)]
        public string? PaymentMode { get; set; }

        [StringLength(100)]
        public string? ReferenceId { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public bool IsEmailSent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaymentCompletionTime { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
