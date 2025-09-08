using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace coptic_app_backend.Infrastructure.Services
{
    public class AwsCognitoService : ICognitoUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AwsCognitoService> _logger;
        private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
        private readonly string _userPoolId;
        private readonly string _clientId;

        public AwsCognitoService(IUserRepository userRepository, IConfiguration configuration, ILogger<AwsCognitoService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;

            // Get AWS credentials from environment variables or configuration
            var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? _configuration["AWS:AccessKeyId"];
            var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? _configuration["AWS:SecretAccessKey"];
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? _configuration["AWS:Region"];
            _userPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? _configuration["Cognito:UserPoolId"] ?? "";
            _clientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? _configuration["Cognito:ClientId"] ?? "";

            // Log configuration status but don't throw exceptions in constructor
            _logger.LogInformation("AWS Configuration Status - AccessKey: {HasAccessKey}, SecretKey: {HasSecretKey}, Region: {Region}, UserPoolId: {HasUserPoolId}, ClientId: {HasClientId}", 
                !string.IsNullOrEmpty(accessKeyId) ? "SET" : "MISSING", 
                !string.IsNullOrEmpty(secretAccessKey) ? "SET" : "MISSING",
                region ?? "DEFAULT(us-east-1)",
                !string.IsNullOrEmpty(_userPoolId) ? "SET" : "MISSING",
                !string.IsNullOrEmpty(_clientId) ? "SET" : "MISSING");

            // Only create Cognito client if we have valid configuration
            if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey) && 
                !string.IsNullOrEmpty(_userPoolId) && !string.IsNullOrEmpty(_clientId))
            {
                try
                {
                    var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKeyId, secretAccessKey);
                    var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region ?? "us-east-1");
                    _cognitoClient = new AmazonCognitoIdentityProviderClient(credentials, regionEndpoint);
                    _logger.LogInformation("AWS Cognito service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize AWS Cognito client, will operate in local-only mode");
                    _cognitoClient = null;
                }
            }
            else
            {
                _logger.LogWarning("AWS Cognito configuration incomplete, operating in local-only mode");
                _cognitoClient = null;
            }
        }

        public async Task<CognitoResult> RegisterUserAsync(string email, string password, string? name, string? phoneNumber, string? deviceToken = null, string? abuneId = null)
        {
            try
            {
                // Check if Cognito is properly configured
                if (_cognitoClient == null)
                {
                    _logger.LogError("Cannot register user - AWS Cognito is not properly configured. Email: {Email}", email);
                    return new CognitoResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "Email verification service is unavailable. Please contact support." 
                    };
                }

                // Register user in Cognito first - THIS IS REQUIRED
                var signUpRequest = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = email,
                    Password = password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType { Name = "email", Value = email },
                        new AttributeType { Name = "name", Value = name ?? "" }
                    }
                };

                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    var formattedPhone = FormatPhoneNumberForCognito(phoneNumber);
                    signUpRequest.UserAttributes.Add(new AttributeType { Name = "phone_number", Value = formattedPhone });
                }

                var signUpResponse = await _cognitoClient.SignUpAsync(signUpRequest);

                _logger.LogInformation("Cognito registration successful for {Email}. UserSub: {UserSub}, CodeDeliveryDetails: {CodeDeliveryDetails}", 
                    email, signUpResponse.UserSub, signUpResponse.CodeDeliveryDetails?.Destination);

                // Validate that Cognito actually sent the verification email
                if (signUpResponse.CodeDeliveryDetails == null)
                {
                    _logger.LogError("Cognito registration succeeded but no verification email was sent for {Email}", email);
                    return new CognitoResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "Registration succeeded but email verification failed. Please contact support." 
                    };
                }

                // Only proceed with local database if Cognito registration is successful
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = email,
                    Email = email,
                    Name = name,
                    PhoneNumber = phoneNumber,
                    PasswordHash = HashPassword(password),
                    UserType = Domain.Models.UserType.Regular,
                    UserStatus = UserStatus.PendingApproval,
                    AbuneId = abuneId,
                    EmailVerified = false, // Will be set to true when user confirms email
                    PhoneNumberVerified = false,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var createdUser = await _userRepository.CreateUserAsync(user);

                _logger.LogInformation("User registered successfully in both Cognito and local database: {Email}", email);
                return new CognitoResult 
                { 
                    IsSuccess = true, 
                    UserId = createdUser.Id,
                    CognitoSub = signUpResponse.UserSub,
                    RequiresEmailVerification = true,
                    Message = "User registered successfully. Please check your email for verification code."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user in Cognito: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoResult> RegisterAbuneAsync(string email, string password, string? name, string? churchName, string? location = null, string? bio = null)
        {
            try
            {
                // Register Abune in Cognito first
                var signUpRequest = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = email,
                    Password = password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType { Name = "email", Value = email },
                        new AttributeType { Name = "name", Value = name ?? "" }
                    }
                };

                var signUpResponse = await _cognitoClient.SignUpAsync(signUpRequest);

                // Only proceed with local database if Cognito registration is successful
                var abune = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = email,
                    Email = email,
                    Name = name,
                    PasswordHash = HashPassword(password),
                    UserType = Domain.Models.UserType.Abune,
                    UserStatus = UserStatus.Active,
                    ChurchName = churchName,
                    Location = location,
                    Bio = bio,
                    EmailVerified = false,
                    PhoneNumberVerified = false,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var createdAbune = await _userRepository.CreateUserAsync(abune);

                _logger.LogInformation("Abune registered successfully in both Cognito and local database: {Email}", email);
                return new CognitoResult 
                { 
                    IsSuccess = true, 
                    UserId = createdAbune.Id,
                    CognitoSub = signUpResponse.UserSub
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering Abune in Cognito: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoAuthResult> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                // Get user from local database
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found with email {Email}", email);
                    return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Invalid email or password" };
                }

                // Verify password using local hash
                var isPasswordValid = VerifyPassword(password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for user {Email}", email);
                    return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Invalid email or password" };
                }

                // Generate JWT token locally
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User authenticated successfully: {Email}", email);
                return new CognitoAuthResult
                {
                    IsSuccess = true,
                    AccessToken = token,
                    RefreshToken = Guid.NewGuid().ToString(), // Simple refresh token
                    IdToken = token,
                    ExpiresIn = 3600 // 1 hour
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
            try
            {
                var confirmRequest = new ConfirmSignUpRequest
                {
                    ClientId = _clientId,
                    Username = email,
                    ConfirmationCode = confirmationCode
                };

                await _cognitoClient.ConfirmSignUpAsync(confirmRequest);

                // Update local database to mark email as verified
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user != null)
                {
                    user.EmailVerified = true;
                    user.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await _userRepository.UpdateUserAsync(user.Id!, user);
                    _logger.LogInformation("Email verified status updated in local database for user: {Email}", email);
                }

                _logger.LogInformation("Email confirmed successfully for user: {Email}", email);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming user email: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoAuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var refreshRequest = new InitiateAuthRequest
                {
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        { "REFRESH_TOKEN", refreshToken }
                    }
                };

                var refreshResponse = await _cognitoClient.InitiateAuthAsync(refreshRequest);

                if (refreshResponse.AuthenticationResult != null)
                {
                    return new CognitoAuthResult
                    {
                        IsSuccess = true,
                        AccessToken = refreshResponse.AuthenticationResult.AccessToken,
                        RefreshToken = refreshResponse.AuthenticationResult.RefreshToken,
                        IdToken = refreshResponse.AuthenticationResult.IdToken,
                        ExpiresIn = refreshResponse.AuthenticationResult.ExpiresIn
                    };
                }

                return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Token refresh failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new CognitoAuthResult { IsSuccess = false, ErrorMessage = "Token refresh failed" };
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
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"] ?? "this-is-a-very-long-secret-key-for-jwt-signing-that-is-at-least-64-characters-long-for-security";
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["Jwt:Issuer"] ?? "coptic-app-backend";
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["Jwt:Audience"] ?? "coptic-app-frontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id!),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", user.Email),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", user.Name ?? ""),
                new Claim("UserId", user.Id!),
                new Claim("UserType", user.UserType.ToString()),
                new Claim("AbuneId", user.UserType == Domain.Models.UserType.Abune ? user.Id : (user.AbuneId ?? ""))
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string FormatPhoneNumberForCognito(string phoneNumber)
        {
            // Remove all non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // If it doesn't start with +, add it
            if (!phoneNumber.StartsWith("+"))
            {
                return "+" + digits;
            }
            
            return phoneNumber;
        }

        public async Task<CognitoResult> InitiatePasswordResetAsync(string email)
        {
            try
            {
                var forgotPasswordRequest = new ForgotPasswordRequest
                {
                    ClientId = _clientId,
                    Username = email
                };

                await _cognitoClient.ForgotPasswordAsync(forgotPasswordRequest);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset for user: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoResult> ResetPasswordAsync(string email, string confirmationCode, string newPassword)
        {
            try
            {
                var confirmForgotPasswordRequest = new ConfirmForgotPasswordRequest
                {
                    ClientId = _clientId,
                    Username = email,
                    ConfirmationCode = confirmationCode,
                    Password = newPassword
                };

                await _cognitoClient.ConfirmForgotPasswordAsync(confirmForgotPasswordRequest);
                
                // Also update the local database password hash to keep them in sync
                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user != null)
                {
                    var hashedPassword = HashPassword(newPassword);
                    user.PasswordHash = hashedPassword;
                    await _userRepository.UpdateUserAsync(user.Id, user);
                    _logger.LogInformation("Password reset successfully updated in both Cognito and local database for user: {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Password reset successful in Cognito but user not found in local database: {Email}", email);
                }
                
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoResult> LogoutUserAsync(string accessToken)
        {
            try
            {
                var globalSignOutRequest = new GlobalSignOutRequest
                {
                    AccessToken = accessToken
                };

                await _cognitoClient.GlobalSignOutAsync(globalSignOutRequest);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out user");
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoResult> ResendConfirmationCodeAsync(string email)
        {
            try
            {
                var resendConfirmationCodeRequest = new ResendConfirmationCodeRequest
                {
                    ClientId = _clientId,
                    Username = email
                };

                await _cognitoClient.ResendConfirmationCodeAsync(resendConfirmationCodeRequest);
                return new CognitoResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation code for user: {Email}", email);
                return new CognitoResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<CognitoUserAttributes?> GetUserAttributesAsync(string email)
        {
            try
            {
                var adminGetUserRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };

                var response = await _cognitoClient.AdminGetUserAsync(adminGetUserRequest);
                
                var attributes = new CognitoUserAttributes();
                foreach (var attr in response.UserAttributes)
                {
                    switch (attr.Name)
                    {
                        case "name":
                            attributes.Name = attr.Value;
                            break;
                        case "gender":
                            attributes.Gender = attr.Value;
                            break;
                        case "email":
                            attributes.Email = attr.Value;
                            break;
                    }
                }

                return attributes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user attributes for: {Email}", email);
                return null;
            }
        }
    }
}



