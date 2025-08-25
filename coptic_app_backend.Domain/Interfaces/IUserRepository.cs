using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetUsersAsync();
        Task<List<User>> GetUsersByAbuneIdAsync(string abuneId);
        Task<User> CreateUserAsync(User user);
        Task<User> UpsertUserAsync(User user);
        Task<User> UpdateUserAsync(string userId, User user);
        Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken);
        Task<User?> GetUserByIdAsync(string userId);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
