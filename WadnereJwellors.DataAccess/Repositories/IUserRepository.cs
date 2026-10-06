using System.Collections.Generic;
using System.Threading.Tasks;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.DataAccess.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);

        Task<UserRegistration> AddRegistrationAsync(UserRegistration registration);
        Task UpdateRegistrationAsync(UserRegistration registration);
        Task<UserRegistration?> GetByRegistrationIdAsync(int id);
        Task<UserRegistration?> GetByMobileNumberAsync(long mobileNumber);
        Task<IEnumerable<UserRegistration>> GetAllRegisterUserAsync();

        // OTP Methods
        Task SaveOtpAsync(UserOtp otp);
        Task<UserOtp?> GetValidOtpAsync(long mobileNumber, string otpCode);
        Task MarkOtpUsedAsync(int otpId);

        // Refresh Token Methods
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
    }
}
