namespace WadnereJwellors.Business.DTOs
{
    public class MonthlyPaymentRequestDto
    {
        public int RegistrationId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }
}
