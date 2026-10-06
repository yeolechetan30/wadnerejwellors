using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WadnereJwellors.DataAccess.Context;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync() {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id) {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> AddAsync(User user) {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user) {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id) {
            var user = await _context.Users.FindAsync(id);
            if (user != null) {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<UserRegistration>> GetAllRegisterUserAsync() {
            return await _context.UserRegistrations.AsNoTracking().ToListAsync();
        }

        public async Task<UserRegistration> AddRegistrationAsync(UserRegistration registration) {
            await _context.UserRegistrations.AddAsync(registration);
            await _context.SaveChangesAsync();
            return registration;
        }

        public async Task UpdateRegistrationAsync(UserRegistration registration) {
            _context.UserRegistrations.Update(registration);
            await _context.SaveChangesAsync();
        }

        public async Task<UserRegistration?> GetByRegistrationIdAsync(int id) {
            return await _context.UserRegistrations.FindAsync(id);
        }

        public async Task<UserRegistration?> GetByMobileNumberAsync(long mobileNumber) {
            return await _context.UserRegistrations
                .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
        }

        // OTP Methods Implementation
        public async Task SaveOtpAsync(UserOtp otp) {
            await _context.UserOtps.AddAsync(otp);
            await _context.SaveChangesAsync();
        }

        public async Task<UserOtp?> GetValidOtpAsync(long mobileNumber, string otpCode) {
            return await _context.UserOtps
                .Where(o => o.MobileNumber == mobileNumber 
                         && o.OtpCode == otpCode 
                         && !o.IsUsed 
                         && o.ExpiryTime > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task MarkOtpUsedAsync(int otpId) {
            var otp = await _context.UserOtps.FindAsync(otpId);
            if (otp != null) {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }

        // Refresh Token Methods Implementation
        public async Task SaveRefreshTokenAsync(RefreshToken refreshToken) {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token) {
            return await _context.RefreshTokens
                .Include(r => r.UserRegistration)
                .FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);
        }

        public async Task RevokeRefreshTokenAsync(string token) {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
            if (refreshToken != null) {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
