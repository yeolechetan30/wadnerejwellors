using System.Threading.Tasks;

namespace WadnereJwellors.Business.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendOtpViaWhatsAppAsync(long mobileNumber, string otpCode);
    }
}
