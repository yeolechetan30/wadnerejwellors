using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Repositories;
using WadnereJwellors.Domain.Entities;
using WadnereJwellors.Domain.Exceptions;

namespace WadnereJwellors.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IWhatsAppService whatsAppService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _whatsAppService = whatsAppService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByMobileNumberAsync(request.MobileNumber);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid mobile number or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your user account is inactive. Please contact support.");
            }

            bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid mobile number or password.");
            }

            // 1. Generate 6-digit numeric OTP for 2FA
            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

            // 2. Save 6-digit OTP record
            var userOtp = new UserOtp
            {
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                OtpCode = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveOtpAsync(userOtp);

            // 3. Send 6-digit OTP to WhatsApp on user's mobile number
            await _whatsAppService.SendOtpViaWhatsAppAsync(user.MobileNumber, otpCode);

            // 4. Send 6-digit OTP to user's registered Email Address if present
            if (!string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                await _emailService.SendOtpEmailAsync(user.EmailAddress, fullName, otpCode, "Login 2FA Verification");
            }

            // 5. Generate JWT Access Token & Refresh Token
            var (accessToken, expiration) = _jwtTokenService.GenerateAccessToken(user);
            string refreshTokenStr = _jwtTokenService.GenerateRefreshToken();

            int refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days) ? days : 7;
            var refreshTokenEntity = new RefreshToken
            {
                RegistrationId = user.RegistrationId,
                Token = refreshTokenStr,
                ExpiryDate = DateTime.UtcNow.AddDays(refreshDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveRefreshTokenAsync(refreshTokenEntity);

            string deliveryChannels = !string.IsNullOrWhiteSpace(user.EmailAddress)
                ? "WhatsApp and your registered email address"
                : "WhatsApp number";

            return new AuthResponseDto
            {
                Success = true,
                Message = $"Authentication successful. 6-digit 2FA OTP code has been sent to your {deliveryChannels}.",
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                TokenExpiresAt = expiration,
                RequiresOtpVerification = true,
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                FullName = $"{user.FirstName} {user.LastName}",
                EmailAddress = user.EmailAddress ?? string.Empty,
                IsAdmin = user.IsAdmin
            };
        }

        public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            var validOtp = await _userRepository.GetValidOtpAsync(request.MobileNumber, request.OtpCode);
            if (validOtp == null)
            {
                throw new InvalidOperationException("Invalid or expired 6-digit OTP code.");
            }

            // Mark OTP as used
            await _userRepository.MarkOtpUsedAsync(validOtp.Id);

            var user = await _userRepository.GetByMobileNumberAsync(request.MobileNumber);
            if (user == null)
            {
                throw new NotFoundException(nameof(UserRegistration), request.MobileNumber);
            }

            if (!user.IsMobileVerified)
            {
                user.IsMobileVerified = true;
                await _userRepository.UpdateRegistrationAsync(user);
            }

            // Issue new Tokens upon successful 2FA OTP verification
            var (accessToken, expiration) = _jwtTokenService.GenerateAccessToken(user);
            string refreshTokenStr = _jwtTokenService.GenerateRefreshToken();

            int refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days) ? days : 7;
            var refreshTokenEntity = new RefreshToken
            {
                RegistrationId = user.RegistrationId,
                Token = refreshTokenStr,
                ExpiryDate = DateTime.UtcNow.AddDays(refreshDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveRefreshTokenAsync(refreshTokenEntity);

            return new AuthResponseDto
            {
                Success = true,
                Message = "2FA OTP verification successful. Login complete.",
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                TokenExpiresAt = expiration,
                RequiresOtpVerification = false,
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                FullName = $"{user.FirstName} {user.LastName}",
                EmailAddress = user.EmailAddress ?? string.Empty,
                IsAdmin = user.IsAdmin
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                throw new InvalidOperationException("Invalid access token.");
            }

            var registrationIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;

            if (!int.TryParse(registrationIdClaim, out int registrationId))
            {
                throw new InvalidOperationException("Invalid token claims.");
            }

            var savedRefreshToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken);
            if (savedRefreshToken == null || savedRefreshToken.RegistrationId != registrationId || savedRefreshToken.ExpiryDate <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // Revoke current refresh token
            await _userRepository.RevokeRefreshTokenAsync(request.RefreshToken);

            var user = await _userRepository.GetByRegistrationIdAsync(registrationId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            // Generate new Access and Refresh tokens
            var (newAccessToken, expiration) = _jwtTokenService.GenerateAccessToken(user);
            string newRefreshTokenStr = _jwtTokenService.GenerateRefreshToken();

            int refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days) ? days : 7;
            var newRefreshTokenEntity = new RefreshToken
            {
                RegistrationId = user.RegistrationId,
                Token = newRefreshTokenStr,
                ExpiryDate = DateTime.UtcNow.AddDays(refreshDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveRefreshTokenAsync(newRefreshTokenEntity);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Tokens refreshed successfully.",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenStr,
                TokenExpiresAt = expiration,
                RequiresOtpVerification = false,
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                FullName = $"{user.FirstName} {user.LastName}",
                EmailAddress = user.EmailAddress ?? string.Empty,
                IsAdmin = user.IsAdmin
            };
        }

        public async Task<bool> ResendOtpAsync(long mobileNumber)
        {
            var user = await _userRepository.GetByMobileNumberAsync(mobileNumber);
            if (user == null || !user.IsActive)
            {
                throw new NotFoundException("Active user registration with mobile number", mobileNumber);
            }

            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            var userOtp = new UserOtp
            {
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                OtpCode = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveOtpAsync(userOtp);

            var whatsAppSent = await _whatsAppService.SendOtpViaWhatsAppAsync(mobileNumber, otpCode);

            if (!string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                await _emailService.SendOtpEmailAsync(user.EmailAddress, fullName, otpCode, "Login 2FA Verification");
            }

            return whatsAppSent;
        }

        public async Task<bool> UpdatePasswordAsync(UpdatePasswordRequestDto request)
        {
            var user = await _userRepository.GetByMobileNumberAsync(request.MobileNumber);
            if (user == null)
            {
                throw new NotFoundException("User registration with mobile number", request.MobileNumber);
            }

            bool isCurrentPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new InvalidOperationException("New password and confirm password do not match.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.DateModified = DateTime.UtcNow;

            await _userRepository.UpdateRegistrationAsync(user);
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetByMobileNumberAsync(request.MobileNumber);
            if (user == null || !user.IsActive)
            {
                throw new NotFoundException("Active user registration with mobile number", request.MobileNumber);
            }

            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            var userOtp = new UserOtp
            {
                RegistrationId = user.RegistrationId,
                MobileNumber = user.MobileNumber,
                OtpCode = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.SaveOtpAsync(userOtp);

            var whatsAppSent = await _whatsAppService.SendOtpViaWhatsAppAsync(user.MobileNumber, otpCode);

            if (!string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                await _emailService.SendOtpEmailAsync(user.EmailAddress, fullName, otpCode, "Password Reset");
            }

            return whatsAppSent;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new InvalidOperationException("New password and confirm password do not match.");
            }

            var validOtp = await _userRepository.GetValidOtpAsync(request.MobileNumber, request.OtpCode);
            if (validOtp == null)
            {
                throw new InvalidOperationException("Invalid or expired 6-digit OTP code.");
            }

            await _userRepository.MarkOtpUsedAsync(validOtp.Id);

            var user = await _userRepository.GetByMobileNumberAsync(request.MobileNumber);
            if (user == null)
            {
                throw new NotFoundException("User registration with mobile number", request.MobileNumber);
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.DateModified = DateTime.UtcNow;

            await _userRepository.UpdateRegistrationAsync(user);
            return true;
        }
    }
}
