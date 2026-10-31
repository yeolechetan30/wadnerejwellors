namespace WadnereJwellors.Business.DTOs
{
    public class CreatePaymentRequestDto
    {
        public decimal Amount { get; set; }
        public int? RegistrationId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? OrderNote { get; set; }
    }

    public class PaymentOrderResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string? PaymentSessionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }

    public class PaymentStatusDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceId { get; set; }
    }
}
