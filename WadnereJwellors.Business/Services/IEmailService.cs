using System.Threading.Tasks;

namespace WadnereJwellors.Business.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string purpose = "Login Verification");
        Task<bool> SendTransactionReceiptEmailAsync(string toEmail, string customerName, string orderId, decimal amount, string status, string? paymentMode = null, string? referenceId = null, string? failureReason = null);
    }
}
