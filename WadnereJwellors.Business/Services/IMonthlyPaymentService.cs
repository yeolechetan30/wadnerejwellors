using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface IMonthlyPaymentService
    {
        Task<PaymentOrderResponseDto> InitiateMonthlyPaymentOrderAsync(MonthlyPaymentRequestDto request);
        Task<int> RecordSuccessfulPaymentAsync(int registrationId, int transactionId, decimal amount, string? remarks = null);
        Task<UserPassbookResponseDto> GetPassbookAsync(int registrationId);
        Task<bool> RedeemMonthlyPaymentAsync(int paymentId);
    }
}

