using System.Security.Cryptography;
using System.Text;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Infrastructure.Services
{
    public class SimpleAuthService : ICognitoUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleAuthService> _logger;

        public SimpleAuthService(IUserRepository userRepository, IConfiguration configuration, ILogger<SimpleAuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CognitoResult> RegisterUserAsync(string email, string password, string? name, string? phoneNumber, string? deviceToken = null, string? abuneId = null)
        {
            try
            {
                // Hash the password
                var hashedPassword = HashPassword(password);

                // Create or update user (upsert)
                var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var user = new User
                {
                    Username = email,
                    Email = email,
                    Name = name ?? email,
                    PhoneNumber = phoneNumber,
                    DeviceToken = deviceToken, // Set during registration or null if not provided
                    PasswordHash = hashedPassword,
                    CreatedAt = currentTimestamp,
                    LastModified = currentTimestamp,
                    UserType = UserType.Regular, // Default to regular user
                    UserStatus = UserStatus.PendingApproval, // Requires Abune approval
                    AbuneId = abuneId, // Link to specific Abune
                    IsApproved = false // Not approved until Abune approves
                };

                var createdUser = await _userRepository.UpsertUserAsync(user);

                _logger.LogInformation("Regular user registered successfully: {Email} under Abune: {AbuneId}", email, abuneId);
                return new CognitoResult { IsSuccess = true, UserId = createdUser.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = "Registration failed" };
            }
        }

        public async Task<CognitoResult> RegisterAbuneAsync(string email, string password, string? name, string? churchName, string? location = null, string? bio = null)
        {
            try
            {
                // Hash the password
                var hashedPassword = HashPassword(password);

                // Create Abune user
                var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var abuneUser = new User
                {
                    Username = email,
                    Email = email,
                    Name = name ?? email,
                    PasswordHash = hashedPassword,
                    DeviceToken = null, // Will be set when Abune logs in from mobile app
                    CreatedAt = currentTimestamp,
                    LastModified = currentTimestamp,
                    UserType = UserType.Abune, // Abune user
                    UserStatus = UserStatus.Active, // Abune users are automatically active
                    AbuneId = null, // Abune users don't belong to other Abunes
                    IsApproved = true, // Abune users are automatically approved
                    ChurchName = churchName,
                    Location = location,
                    Bio = bio
                };

                var createdAbune = await _userRepository.UpsertUserAsync(abuneUser);

                _logger.LogInformation("Abune user registered successfully: {Email} for church: {ChurchName}", email, churchName);
                return new CognitoResult { IsSuccess = true, UserId = createdAbune.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering Abune user: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = "Abune registration failed" };
            }
        }

        public async Task<CognitoAuthResult> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                // Get user by email
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found with email {Email}", email);
                    return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Invalid email or password" };
                }

                _logger.LogDebug("User found: {Email}, UserId: {UserId}, StoredHash: {Hash}", email, user.Id, user.PasswordHash);

                // Verify password
                var isPasswordValid = VerifyPassword(password, user.PasswordHash);
                _logger.LogDebug("Password verification result: {IsValid} for user {Email}", isPasswordValid, email);
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for user {Email}", email);
                    return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Invalid email or password" };
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User authenticated successfully: {Email}", email);
                return new CognitoAuthResult
                {
                    IsSuccess = true,
                    AccessToken = token,
                    RefreshToken = Guid.NewGuid().ToString(), // Simple refresh token
                    ExpiresIn = 3600, // 1 hour
                    IdToken = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user: {Email}", email);
                return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Authentication failed" };
            }
        }

        public async Task<CognitoResult> ConfirmUserAsync(string email, string confirmationCode)
        {
            // For simple auth, we'll just return success
            // In a real implementation, you might want to implement email confirmation
            return new CognitoResult { IsSuccess = true };
        }

        public async Task<CognitoAuthResult> RefreshTokenAsync(string refreshToken)
        {
            // For simple auth, we'll just return success
            // In a real implementation, you'd validate the refresh token
            return new CognitoAuthResult { IsSuccess = true, AccessToken = "new_token", ExpiresIn = 3600 };
        }

        public async Task<CognitoResult> InitiatePasswordResetAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new CognitoResult { IsSuccess = false, ErrorMessage = "User not found" };
                }

                // In a real implementation, you'd send a password reset email
                // For now, we'll just return success
                _logger.LogInformation("Password reset initiated for user: {Email}", email);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = "Password reset failed" };
            }
        }

        public async Task<CognitoResult> ResetPasswordAsync(string email, string confirmationCode, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return new CognitoResult { IsSuccess = false, ErrorMessage = "User not found" };
                }

                // Hash the new password
                var hashedPassword = HashPassword(newPassword);
                user.PasswordHash = hashedPassword;

                // Update user
                await _userRepository.UpdateUserAsync(user.Id, user);

                _logger.LogInformation("Password reset successfully for user: {Email}", email);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = "Password reset failed" };
            }
        }

        public async Task<CognitoResult> ResendConfirmationCodeAsync(string email)
        {
            // For simple auth, we'll just return success
            return new CognitoResult { IsSuccess = true };
        }

        public async Task<CognitoResult> LogoutUserAsync(string accessToken)
        {
            // For simple auth, we'll just return success
            // In a real implementation, you might want to blacklist the token
            return new CognitoResult { IsSuccess = true };
        }

        public async Task<CognitoUserAttributes?> GetUserAttributesAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return null;
                }

                return new CognitoUserAttributes
                {
                    Name = user.Name,
                    Gender = user.Gender,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user attributes: {Email}", email);
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                // Hash the input password and compare with stored hash
                var hashedPassword = HashPassword(password);
                var isValid = hashedPassword == hash;
                
                _logger.LogDebug("Password verification: Input='{Password}', InputHash='{InputHash}', StoredHash='{StoredHash}', Match={IsValid}", 
                    password, hashedPassword, hash, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

            private string GenerateJwtToken(User user)
    {
        // Use the configuration to get JWT settings
        var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-key-with-at-least-32-characters";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserType", user.UserType.ToString()),
                new Claim("UserId", user.Id), // Add explicit UserId claim
                new Claim("AbuneId", user.AbuneId ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "coptic-app-backend",
                audience: _configuration["Jwt:Audience"] ?? "coptic-app-frontend",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
