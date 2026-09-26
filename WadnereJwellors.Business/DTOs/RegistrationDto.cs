using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WadnereJwellors.Business.DTOs
{
    public class RegistrationDto
    {
        public int RegistrationId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public long MobileNumber { get; set; }
        public bool IsMobileVerified { get; set; }
        public string PasswordHash { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Village_City { get; set; }
        public string Taluka { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public int Pincode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? AnniversaryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
    }
}
