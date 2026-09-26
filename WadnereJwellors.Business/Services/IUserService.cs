using System.Collections.Generic;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int id);

        Task RegisterUserAsync(RegistrationDto register);
        Task UpdateRegisterUserAsync(RegistrationDto register);

        Task<IEnumerable<RegistrationDto>> GetAllRegisterUsersAsync();
    }
}
