using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;
using WadnereJwellors.DataAccess.Repositories;
using WadnereJwellors.Domain.Entities;
using WadnereJwellors.Domain.Exceptions;

namespace WadnereJwellors.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher) {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync() {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserDto> GetUserByIdAsync(int id) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) {
                throw new NotFoundException(nameof(User), id);
            }

            return MapToDto(user);
        }

        private static UserDto MapToDto(User user) {
            return new UserDto {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }

        public async Task<IEnumerable<RegistrationDto>> GetAllRegisterUsersAsync() {
            var registerUsers= await _userRepository.GetAllRegisterUserAsync();
            return registerUsers.Select(MapToDto);
        }


        private static RegistrationDto MapToDto(UserRegistration register) {
            return new RegistrationDto {
                RegistrationId = register.RegistrationId,
                FirstName = register.FirstName,
                LastName = register.LastName,
                EmailAddress = register.EmailAddress,
                MobileNumber = register.MobileNumber,
                IsMobileVerified = register.IsMobileVerified,
                AddressLine1 = register.AddressLine1,
                AddressLine2 = register.AddressLine2,
                Village_City = register.Village_City,
                Taluka = register.Taluka,
                District = register.District,
                State = register.State,
                Pincode = int.TryParse(register.Pincode, out var pin) ? pin : 0,
                DateOfBirth = register.DateOfBirth,
                AnniversaryDate = register.AnniversaryDate,
                IsActive = register.IsActive,
                IsAdmin = register.IsAdmin
            };
        }

        public async Task RegisterUserAsync(RegistrationDto register) {
            var userRegistration = new UserRegistration {
                FirstName = register.FirstName ?? string.Empty,
                LastName = register.LastName ?? string.Empty,
                EmailAddress = register.EmailAddress,
                MobileNumber = register.MobileNumber,
                IsMobileVerified = register.IsMobileVerified,
                PasswordHash = !string.IsNullOrWhiteSpace(register.PasswordHash) 
                    ? _passwordHasher.HashPassword(register.PasswordHash) 
                    : string.Empty,
                AddressLine1 = register.AddressLine1 ?? string.Empty,
                AddressLine2 = register.AddressLine2,
                Village_City = register.Village_City ?? string.Empty,
                Taluka = register.Taluka ?? string.Empty,
                District = register.District ?? string.Empty,
                State = register.State ?? "Maharashtra",
                Pincode = register.Pincode.ToString(),
                DateOfBirth = register.DateOfBirth,
                AnniversaryDate = register.AnniversaryDate,
                IsActive = register.IsActive,
                IsAdmin = register.IsAdmin,
                DateCreated = DateTime.UtcNow
            };

            await _userRepository.AddRegistrationAsync(userRegistration);
        }

        public async Task UpdateRegisterUserAsync(RegistrationDto register) {
            var existingUser = await _userRepository.GetByRegistrationIdAsync(register.RegistrationId);
            if (existingUser == null) {
                throw new NotFoundException(nameof(UserRegistration), register.RegistrationId);
            }

            if (!string.IsNullOrWhiteSpace(register.FirstName))
                existingUser.FirstName = register.FirstName;

            if (!string.IsNullOrWhiteSpace(register.LastName))
                existingUser.LastName = register.LastName;

            if (register.EmailAddress != null)
                existingUser.EmailAddress = register.EmailAddress;

            if (register.MobileNumber != 0) {
                existingUser.MobileNumber = register.MobileNumber;
            }

            // PasswordHash is NOT mandatory for update. Only update if a new password is provided.
            if (!string.IsNullOrWhiteSpace(register.PasswordHash)) {
                existingUser.PasswordHash = _passwordHasher.HashPassword(register.PasswordHash);
            }

            if (!string.IsNullOrWhiteSpace(register.AddressLine1))
                existingUser.AddressLine1 = register.AddressLine1;

            if (register.AddressLine2 != null)
                existingUser.AddressLine2 = register.AddressLine2;

            if (!string.IsNullOrWhiteSpace(register.Village_City))
                existingUser.Village_City = register.Village_City;

            if (!string.IsNullOrWhiteSpace(register.Taluka))
                existingUser.Taluka = register.Taluka;

            if (!string.IsNullOrWhiteSpace(register.District))
                existingUser.District = register.District;

            if (!string.IsNullOrWhiteSpace(register.State))
                existingUser.State = register.State;

            if (register.Pincode != 0)
                existingUser.Pincode = register.Pincode.ToString();

            if (register.DateOfBirth.HasValue)
                existingUser.DateOfBirth = register.DateOfBirth;

            if (register.AnniversaryDate.HasValue)
                existingUser.AnniversaryDate = register.AnniversaryDate;

            existingUser.IsActive = register.IsActive;
            existingUser.IsAdmin = register.IsAdmin;
            existingUser.DateModified = DateTime.UtcNow;

            await _userRepository.UpdateRegistrationAsync(existingUser);
        }
    }
}
