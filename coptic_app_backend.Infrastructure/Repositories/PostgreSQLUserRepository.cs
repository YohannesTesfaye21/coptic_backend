using Microsoft.EntityFrameworkCore;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Infrastructure.Repositories
{
    public class PostgreSQLUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostgreSQLUserRepository> _logger;

        public PostgreSQLUserRepository(ApplicationDbContext context, ILogger<PostgreSQLUserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                // Check if user with same email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email || u.Username == user.Username);
                
                if (existingUser != null)
                {
                    if (existingUser.Email == user.Email)
                    {
                        throw new InvalidOperationException($"User with email '{user.Email}' already exists");
                    }
                    if (existingUser.Username == user.Username)
                    {
                        throw new InvalidOperationException($"User with username '{user.Username}' already exists");
                    }
                }

                if (string.IsNullOrEmpty(user.Id))
                {
                    user.Id = Guid.NewGuid().ToString();
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(string userId, User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(userId);
                if (existingUser == null)
                {
                    throw new ArgumentException($"User with ID {userId} not found");
                }

                // Update properties
                existingUser.Username = user.Username ?? existingUser.Username;
                existingUser.Email = user.Email ?? existingUser.Email;
                existingUser.Name = user.Name ?? existingUser.Name;
                existingUser.PhoneNumber = user.PhoneNumber ?? existingUser.PhoneNumber;
                existingUser.Gender = user.Gender ?? existingUser.Gender;
                existingUser.DeviceToken = user.DeviceToken ?? existingUser.DeviceToken;
                existingUser.PasswordHash = user.PasswordHash ?? existingUser.PasswordHash;
                existingUser.EmailVerified = user.EmailVerified;
                existingUser.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully: {UserId}", userId);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                throw;
            }
        }

        public async Task<User> UpsertUserAsync(User user)
        {
            try
            {
                // Check if user with same email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);
                
                if (existingUser != null)
                {
                    // Update existing user
                    existingUser.Username = user.Username ?? existingUser.Username;
                    existingUser.Name = user.Name ?? existingUser.Name;
                    existingUser.PhoneNumber = user.PhoneNumber ?? existingUser.PhoneNumber;
                    existingUser.Gender = user.Gender ?? existingUser.Gender;
                    existingUser.DeviceToken = user.DeviceToken ?? existingUser.DeviceToken;
                    existingUser.PasswordHash = user.PasswordHash ?? existingUser.PasswordHash;
                    existingUser.EmailVerified = user.EmailVerified;
                    existingUser.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User updated successfully: {UserId}", existingUser.Id);
                    return existingUser;
                }
                else
                {
                    // Create new user
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        user.Id = Guid.NewGuid().ToString();
                    }
                    
                    user.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    user.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User created successfully: {UserId}", user.Id);
                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting user");
                throw;
            }
        }

        public async Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.DeviceToken = deviceToken;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Device token registered successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted successfully: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists: {Email}", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if username exists: {Username}", username);
                throw;
            }
        }

        public async Task<List<User>> GetUsersByAbuneIdAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.AbuneId == abuneId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by Abune ID: {AbuneId}", abuneId);
                throw;
            }
        }
    }
}
