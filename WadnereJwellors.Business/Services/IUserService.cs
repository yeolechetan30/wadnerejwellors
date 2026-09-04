using System.Collections.Generic;
using System.Threading.Tasks;
using WadnereJwellors.Business.DTOs;

namespace WadnereJwellors.Business.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(int id);
    }
}
