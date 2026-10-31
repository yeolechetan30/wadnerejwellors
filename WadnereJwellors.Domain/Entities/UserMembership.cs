using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WadnereJwellors.Domain.Entities
{
    [Table("UserMemberships")]
    public class UserMembership
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MembershipId { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [ForeignKey("RegistrationId")]
        public virtual UserRegistration? UserRegistration { get; set; }

        [Required]
        [StringLength(30)]
        public string MembershipCode { get; set; } = string.Empty; // mem_20260001

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int? PaymentTransactionId { get; set; }

        [ForeignKey("PaymentTransactionId")]
        public virtual PaymentTransaction? PaymentTransaction { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }
    }
}