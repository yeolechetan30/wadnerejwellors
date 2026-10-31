using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WadnereJwellors.Domain.Entities
{
    [Table("UserRegistration")]
    public class UserRegistration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RegistrationId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? EmailAddress { get; set; }

        public long MobileNumber { get; set; }

        public bool IsMobileVerified { get; set; } = false;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string Village_City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Taluka { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = "Maharashtra";

        [Required]
        [StringLength(10)]
        public string Pincode { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public DateTime? AnniversaryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsAdmin { get; set; } = false;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateModified { get; set; }
    }
}
