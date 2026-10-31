using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WadnereJwellors.Business.DTOs
{
    public class MembershipRequestDto
    {
        public int RegistrationId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class MembershipStatusResponseDto
    {
        public bool HasActiveMembership { get; set; }
        public string? MembershipCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int ValidityYears { get; set; }
        public bool CanRenew { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
