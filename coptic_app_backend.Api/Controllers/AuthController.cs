using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// Authentication controller for user registration, login, and account management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly ICognitoUserService _cognitoUserService;

        public AuthController(IUserService userService, IUserRepository userRepository, ICognitoUserService cognitoUserService)
        {
            _userService = userService;
            _userRepository = userRepository;
            _cognitoUserService = cognitoUserService;
        }

        /// <summary>
        /// Register a new regular user under a specific Abune
        /// </summary>
        /// <param name="request">User registration information including AbuneId</param>
        /// <returns>Registration result with user details</returns>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid registration data or missing AbuneId</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Registration data is required");
                }

                if (string.IsNullOrEmpty(request.FullName))
                {
                    return BadRequest("Full Name is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.Gender))
                {
                    return BadRequest("Gender is required");
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Password is required");
                }

                if (string.IsNullOrEmpty(request.ConfirmPassword))
                {
                    return BadRequest("Confirm Password is required");
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return BadRequest("Password and Confirm Password must match");
                }

                if (request.Password.Length < 8)
                {
                    return BadRequest("Password must be at least 8 characters long");
                }

                // Validate AbuneId is provided for regular user registration
                if (string.IsNullOrEmpty(request.AbuneId))
                {
                    return BadRequest("AbuneId is required for user registration");
                }

                // Create user in local database using authentication service
                var authResult = await _cognitoUserService.RegisterUserAsync(
                    request.Email, 
                    request.Password, 
                    request.FullName, 
                    request.PhoneNumber,
                    request.DeviceToken,
                    request.AbuneId
                );

                if (!authResult.IsSuccess)
                {
                    return BadRequest(new { error = "Registration failed", message = authResult.ErrorMessage });
                }

                var createdUser = await _userRepository.GetUserByIdAsync(authResult.UserId!);

                return Ok(new AuthResponse
                {
                    Message = "User registered successfully. Please wait for Abune approval.",
                    UserId = createdUser!.Id,
                    Email = createdUser.Email,
                    FullName = createdUser.Name,
                    Gender = createdUser.Gender,
                    PhoneNumber = createdUser.PhoneNumber,
                    DeviceToken = createdUser.DeviceToken,
                    UserType = createdUser.UserType.ToString(),
                    IsApproved = createdUser.IsApproved,
                    AbuneId = createdUser.AbuneId,
                    RequiresConfirmation = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication result with JWT token and user information</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid login data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="403">Account not approved or not active</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Login data is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Password is required");
                }

                // Authenticate with AWS Cognito
                var authResult = await _cognitoUserService.AuthenticateUserAsync(request.Email, request.Password);
                
                if (!authResult.IsSuccess)
                {
                    return Unauthorized(new { error = "Authentication failed", message = authResult.ErrorMessage });
                }

                // Get user from local database
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound("User not found in local database");
                }

                // Check if user is approved (for regular users)
                if (user.UserType == UserType.Regular && !user.IsApproved)
                {
                    return StatusCode(403, new { error = "Account pending approval", message = "Your account is pending approval from your Abune" });
                }

                // Check if user is active
                if (user.UserStatus != UserStatus.Active)
                {
                    return StatusCode(403, new { error = "Account not active", message = "Your account is not active. Please contact your Abune." });
                }

                // Update device token if provided in login request
                if (!string.IsNullOrEmpty(request.DeviceToken) && request.DeviceToken != user.DeviceToken)
                {
                    await _userRepository.RegisterDeviceTokenAsync(user.Id!, request.DeviceToken);
                    user.DeviceToken = request.DeviceToken; // Update local user object for response
                }

                return Ok(new AuthResponse
                {
                    Message = "Login successful",
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.Name,
                    Gender = user.Gender,
                    PhoneNumber = user.PhoneNumber,
                    DeviceToken = user.DeviceToken,
                    UserType = user.UserType.ToString(),
                    IsApproved = user.IsApproved,
                    AbuneId = user.AbuneId,
                    AccessToken = authResult.AccessToken,
                    RefreshToken = authResult.RefreshToken,
                    ExpiresIn = authResult.ExpiresIn,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("confirm")]
        public async Task<ActionResult<AuthResponse>> ConfirmRegistration([FromBody] ConfirmRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Confirmation data is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.ConfirmationCode))
                {
                    return BadRequest("Confirmation code is required");
                }

                // Confirm user registration with AWS Cognito
                var confirmResult = await _cognitoUserService.ConfirmUserAsync(request.Email, request.ConfirmationCode);
                
                if (!confirmResult.IsSuccess)
                {
                    return BadRequest(new { error = "Confirmation failed", message = confirmResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Email confirmed successfully. You can now login.",
                    Email = request.Email,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Refresh token is required");
                }

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest("Refresh token is required");
                }

                // Refresh access token with AWS Cognito
                var refreshResult = await _cognitoUserService.RefreshTokenAsync(request.RefreshToken);
                
                if (!refreshResult.IsSuccess)
                {
                    return Unauthorized(new { error = "Token refresh failed", message = refreshResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Token refreshed successfully",
                    AccessToken = refreshResult.AccessToken,
                    RefreshToken = request.RefreshToken,
                    ExpiresIn = refreshResult.ExpiresIn,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Update user's device token for push notifications
        /// </summary>
        /// <param name="request">Device token update request</param>
        /// <returns>Update result</returns>
        /// <response code="200">Device token updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Authentication required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("update-device-token")]
        [Authorize]
        public async Task<ActionResult<AuthResponse>> UpdateDeviceToken([FromBody] UpdateDeviceTokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.DeviceToken))
                {
                    return BadRequest("Device token is required");
                }

                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User ID not found in token");
                }

                var success = await _userRepository.RegisterDeviceTokenAsync(currentUserId, request.DeviceToken);
                if (!success)
                {
                    return BadRequest("Failed to update device token");
                }

                return Ok(new AuthResponse
                {
                    Message = "Device token updated successfully",
                    DeviceToken = request.DeviceToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<AuthResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                // Initiate password reset with AWS Cognito
                var resetResult = await _cognitoUserService.InitiatePasswordResetAsync(request.Email);
                
                if (!resetResult.IsSuccess)
                {
                    return BadRequest(new { error = "Password reset failed", message = resetResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Password reset code sent to your email. Please check your inbox.",
                    Email = request.Email,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("send-confirmation-code")]
        public async Task<ActionResult<AuthResponse>> SendConfirmationCode([FromBody] SendConfirmationCodeRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                // Resend confirmation code with AWS Cognito
                var resendResult = await _cognitoUserService.ResendConfirmationCodeAsync(request.Email);
                
                if (!resendResult.IsSuccess)
                {
                    return BadRequest(new { error = "Failed to send confirmation code", message = resendResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Confirmation code sent to your email. Please check your inbox.",
                    Email = request.Email,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Reset data is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.ConfirmationCode))
                {
                    return BadRequest("Confirmation code is required");
                }

                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest("New password is required");
                }

                if (request.NewPassword.Length < 8)
                {
                    return BadRequest("New password must be at least 8 characters long");
                }

                // Reset password with AWS Cognito
                var resetResult = await _cognitoUserService.ResetPasswordAsync(request.Email, request.ConfirmationCode, request.NewPassword);
                
                if (!resetResult.IsSuccess)
                {
                    return BadRequest(new { error = "Password reset failed", message = resetResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Password reset successfully. You can now login with your new password.",
                    Email = request.Email,
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult<AuthResponse>> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Logout data is required");
                }

                if (string.IsNullOrEmpty(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }

                // Logout user from AWS Cognito
                var logoutResult = await _cognitoUserService.LogoutUserAsync(request.AccessToken);
                
                if (!logoutResult.IsSuccess)
                {
                    return BadRequest(new { error = "Logout failed", message = logoutResult.ErrorMessage });
                }

                return Ok(new AuthResponse
                {
                    Message = "Logged out successfully",
                    RequiresConfirmation = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }

    // Request Models
    /// <summary>
    /// User registration request model
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's full name
        /// </summary>
        /// <example>John Doe</example>
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// User's email address (used as username)
        /// </summary>
        /// <example>john.doe@example.com</example>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// User's gender
        /// </summary>
        /// <example>Male</example>
        public string Gender { get; set; } = string.Empty;
        
        /// <summary>
        /// User's phone number
        /// </summary>
        /// <example>+1234567890</example>
        public string PhoneNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// User's device token for push notifications
        /// </summary>
        /// <example>fcm-device-token-here</example>
        public string? DeviceToken { get; set; }
        
        /// <summary>
        /// User's password (minimum 8 characters)
        /// </summary>
        /// <example>password123</example>
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Password confirmation (must match Password)
        /// </summary>
        /// <example>password123</example>
        public string ConfirmPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the Abune this user belongs to (required for regular users)
        /// </summary>
        /// <example>abune-guid-here</example>
        public string? AbuneId { get; set; } // Required for regular user registration
    }

    /// <summary>
    /// User login request model
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        /// <example>john.doe@example.com</example>
        public string? Email { get; set; }
        
        /// <summary>
        /// User's password
        /// </summary>
        /// <example>password123</example>
        public string? Password { get; set; }
        
        /// <summary>
        /// User's device token for push notifications (optional)
        /// </summary>
        /// <example>fcm-device-token-here</example>
        public string? DeviceToken { get; set; }
    }

    public class ConfirmRequest
    {
        public string? Email { get; set; }
        public string? ConfirmationCode { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    public class SendConfirmationCodeRequest
    {
        public string? Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string? Email { get; set; }
        public string? ConfirmationCode { get; set; }
        public string? NewPassword { get; set; }
    }

    public class LogoutRequest
    {
        public string? AccessToken { get; set; }
    }

    /// <summary>
    /// Device token update request model
    /// </summary>
    public class UpdateDeviceTokenRequest
    {
        /// <summary>
        /// New device token for push notifications
        /// </summary>
        /// <example>fcm-device-token-here</example>
        public string DeviceToken { get; set; } = string.Empty;
    }

    // Response Models
    /// <summary>
    /// Authentication response model
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Response message
        /// </summary>
        /// <example>Login successful</example>
        public string? Message { get; set; }
        
        /// <summary>
        /// User's unique identifier
        /// </summary>
        /// <example>user-guid-here</example>
        public string? UserId { get; set; }
        
        /// <summary>
        /// User's email address
        /// </summary>
        /// <example>john.doe@example.com</example>
        public string? Email { get; set; }
        
        /// <summary>
        /// User's full name
        /// </summary>
        /// <example>John Doe</example>
        public string? FullName { get; set; }
        
        /// <summary>
        /// User's gender
        /// </summary>
        /// <example>Male</example>
        public string? Gender { get; set; }
        
        /// <summary>
        /// User's phone number
        /// </summary>
        /// <example>+1234567890</example>
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// User's device token for push notifications
        /// </summary>
        /// <example>fcm-device-token-here</example>
        public string? DeviceToken { get; set; }
        
        /// <summary>
        /// User type (Regular or Abune)
        /// </summary>
        /// <example>Regular</example>
        public string? UserType { get; set; }
        
        /// <summary>
        /// Whether the user is approved by their Abune
        /// </summary>
        /// <example>false</example>
        public bool? IsApproved { get; set; }
        
        /// <summary>
        /// ID of the Abune this user belongs to
        /// </summary>
        /// <example>abune-guid-here</example>
        public string? AbuneId { get; set; }
        
        /// <summary>
        /// JWT access token for authentication
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string? AccessToken { get; set; }
        
        /// <summary>
        /// JWT refresh token
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string? RefreshToken { get; set; }
        
        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        /// <example>3600</example>
        public int? ExpiresIn { get; set; }
        
        /// <summary>
        /// Whether user confirmation is required
        /// </summary>
        /// <example>false</example>
        public bool RequiresConfirmation { get; set; }
    }
}
