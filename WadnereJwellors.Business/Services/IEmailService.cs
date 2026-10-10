using System.Threading.Tasks;

namespace WadnereJwellors.Business.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string purpose = "Login Verification");
    }
}
