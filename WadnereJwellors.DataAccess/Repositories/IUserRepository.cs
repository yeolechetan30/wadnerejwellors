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
        Task<UserRegistration> GetByRegistrationIdAsync(int id);
        Task<IEnumerable<UserRegistration>> GetAllRegisterUserAsync();
    }
}
