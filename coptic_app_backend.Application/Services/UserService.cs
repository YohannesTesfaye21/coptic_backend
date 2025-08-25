using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public UserService(IUserRepository userRepository, INotificationService notificationService)
        {
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetUsersAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Use upsert logic to handle duplicate emails gracefully
            return await _userRepository.UpsertUserAsync(user);
        }

        public async Task<User> UpsertUserAsync(User user)
        {
            return await _userRepository.UpsertUserAsync(user);
        }

        public async Task<User> UpdateUserAsync(string userId, User user)
        {
            return await _userRepository.UpdateUserAsync(userId, user);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }

        public async Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken)
        {
            return await _userRepository.RegisterDeviceTokenAsync(userId, deviceToken);
        }

        public async Task<bool> TestNotificationAsync(string userId)
        {
            var success = await _notificationService.SendNotificationAsync(userId, "Test Notification", "This is a test notification");
            return success;
        }
    }
}
