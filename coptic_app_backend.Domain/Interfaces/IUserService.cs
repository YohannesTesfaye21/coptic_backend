using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetUsersAsync();
        Task<User> CreateUserAsync(User user);
        Task<User> UpsertUserAsync(User user);
        Task<User> UpdateUserAsync(string userId, User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken);
        Task<bool> TestNotificationAsync(string userId);
    }
}
