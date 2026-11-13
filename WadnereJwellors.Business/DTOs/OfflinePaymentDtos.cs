using System;

namespace WadnereJwellors.Business.DTOs
{
    public class OfflineMembershipPaymentDto
    {
        public int RegistrationId { get; set; }
        public decimal? Amount { get; set; }
        public string? Remarks { get; set; }
    }

    public class OfflineMembershipResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MembershipCode { get; set; } = string.Empty;
        public int RegistrationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class OfflineMonthlyPaymentDto
    {
        public int RegistrationId { get; set; }
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
    }

    public class OfflineMonthlyPaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public int RegistrationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string MonthYearDisplay { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
