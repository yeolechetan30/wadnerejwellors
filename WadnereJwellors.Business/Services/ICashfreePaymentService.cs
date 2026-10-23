using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface ICashfreePaymentService
    {
        Task<PaymentOrderResponseDto> CreateOrderAsync(CreatePaymentRequestDto request);
        Task<PaymentStatusDto> GetOrderStatusAsync(string orderId);
        bool VerifyWebhookSignature(string rawBody, string signature, string timestamp);
        Task ProcessWebhookEventAsync(string rawBody);
    }
}
