using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WadnereJwellors.Business.DTOs
{
    internal class PassbookResponseDto
    {
    }
    public class UserPassbookResponseDto
    {
        public int RegistrationId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? MembershipCode { get; set; }
        public DateTime? MemberSince { get; set; }
        public DateTime? MembershipValidTill { get; set; }
        public bool IsMembershipActive { get; set; }

        // Financial Summary
        public decimal TotalSavedAmount { get; set; }
        public decimal TotalRedeemedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public int TotalPaidMonthsCount { get; set; }

        // Ledger list
        public List<PassbookEntryDto> Entries { get; set; } = new();
    }

    public class PassbookEntryDto
    {
        public int PaymentId { get; set; }
        public string MonthYearDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? OrderId { get; set; }
        public string? ReferenceId { get; set; }
        public bool IsRedeemed { get; set; }
        public DateTime? RedeemedDate { get; set; }
    }

}
