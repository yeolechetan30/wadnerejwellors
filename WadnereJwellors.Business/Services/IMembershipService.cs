using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface IMembershipService
    {
        Task<MembershipStatusResponseDto> CheckMembershipStatusAsync(int registrationId);
        Task<PaymentOrderResponseDto> InitiateMembershipOrderAsync(MembershipRequestDto request);
        Task<string> ActivateMembershipAsync(int registrationId, int transactionId, decimal amount);
    }
}
