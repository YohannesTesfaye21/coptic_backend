using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Api.Attributes;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// User management controller with role-based access control
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IAbuneService _abuneService;
        private readonly INotificationService _notificationService;

        public UserController(IUserService userService, IUserRepository userRepository, IAbuneService abuneService, INotificationService notificationService)
        {
            _userService = userService;
            _userRepository = userRepository;
            _abuneService = abuneService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all users under the current Abune's community (Abune only)
        /// </summary>
        /// <returns>List of community members</returns>
        /// <response code="200">Community members retrieved successfully</response>
        /// <response code="400">Abune ID not found in token</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            try
            {
                // Get AbuneId from JWT claims
                var currentUser = User;
                var currentUserId = currentUser.FindFirst("UserId")?.Value;
                var currentUserType = currentUser.FindFirst("UserType")?.Value;
                
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User ID not found in token");
                }
                
                if (currentUserType != "Abune")
                {
                    return StatusCode(403, "Access denied - Abune only");
                }
                
                // For Abune users, get their own ID as the AbuneId
                var abuneId = currentUserId;

                // Get only users under this Abune
                var users = await _userRepository.GetUsersByAbuneIdAsync(abuneId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("userId is required");
                }

                var currentUser = User;
                var currentUserType = currentUser.FindFirst("UserType")?.Value;
                var currentUserId = currentUser.FindFirst("UserId")?.Value;
                var currentUserAbuneId = currentUser.FindFirst("AbuneId")?.Value;

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Role-based access control
                if (currentUserType == "Abune")
                {
                    // Abune can only see users under their community
                    if (user.AbuneId != currentUserAbuneId)
                    {
                        return StatusCode(403, "You can only view users in your own community");
                    }
                }
                else if (currentUserType == "Regular")
                {
                    // Regular users can see their own profile OR their Abune's profile
                    if (userId != currentUserId && userId != currentUserAbuneId)
                    {
                        return StatusCode(403, "You can only view your own profile or your Abune's profile");
                    }
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("email/{email}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest("email is required");
                }

                var currentUser = User;
                var currentUserType = currentUser.FindFirst("UserType")?.Value;
                var currentUserId = currentUser.FindFirst("UserId")?.Value;
                var currentUserAbuneId = currentUser.FindFirst("AbuneId")?.Value;

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound($"User with email {email} not found");
                }

                // Role-based access control
                if (currentUserType == "Abune")
                {
                    // Abune can only see users under their community
                    if (user.AbuneId != currentUserAbuneId)
                    {
                        return StatusCode(403, "You can only view users in your own community");
                    }
                }
                else if (currentUserType == "Regular")
                {
                    // Regular users can see their own profile OR their Abune's profile
                    if (user.Id != currentUserId && user.Id != currentUserAbuneId)
                    {
                        return StatusCode(403, "You can only view your own profile or your Abune's profile");
                    }
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get the current authenticated user's profile
        /// </summary>
        /// <returns>Current user's profile information</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="400">User ID not found in token</response>
        /// <response code="401">Authentication required</response>
        /// <response code="404">User profile not found</response>
        /// <response code="500">Internal server error</response>
        // GET: api/User/profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<User>> GetCurrentUserProfile()
        {
            try
            {
                var currentUser = User;
                var currentUserId = currentUser.FindFirst("UserId")?.Value;
                
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User ID not found in context");
                }

                var user = await _userRepository.GetUserByIdAsync(currentUserId);
                if (user == null)
                {
                    return NotFound("User profile not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("User data is required");
                }

                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest("Username is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                // Validate that username is a valid email address
                if (!request.Username.Contains("@"))
                {
                    return BadRequest("Username should be an email address");
                }

                // Ensure username and email are the same for Cognito email-based authentication
                if (request.Username != request.Email)
                {
                    return BadRequest("Username and email must be the same for email-based authentication");
                }

                // Check if email already exists
                var emailExists = await _userRepository.EmailExistsAsync(request.Email);
                if (emailExists)
                {
                    return BadRequest("Email already exists. Please use a different email address.");
                }

                // Check if AbuneId exists and is a valid Abune (if provided)
                if (!string.IsNullOrEmpty(request.AbuneId))
                {
                    var abune = await _abuneService.GetAbuneByIdAsync(request.AbuneId);
                    if (abune == null)
                    {
                        return BadRequest("Invalid AbuneId. The specified Abune does not exist.");
                    }
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Name = request.Name,
                    Gender = request.Gender,
                    DeviceToken = request.DeviceToken,
                    AbuneId = request.AbuneId
                };

                var createdUser = await _userService.CreateUserAsync(user);
                return CreatedAtAction(nameof(GetUser), new { userId = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<User>> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("User data is required");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("userId is required");
                }

                // First check if user exists
                var existingUser = await _userRepository.GetUserByIdAsync(userId);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Validate username if provided
                if (!string.IsNullOrEmpty(request.Username))
                {
                    if (!request.Username.Contains("@"))
                    {
                        return BadRequest("Username should be an email address");
                    }
                }

                // Validate email if provided
                if (!string.IsNullOrEmpty(request.Email))
                {
                    if (!request.Email.Contains("@"))
                    {
                        return BadRequest("Email should be a valid email address");
                    }
                }

                // Ensure username and email consistency
                var newUsername = request.Username ?? existingUser.Username;
                var newEmail = request.Email ?? existingUser.Email;
                
                if (newUsername != newEmail)
                {
                    return BadRequest("Username and email must be the same for email-based authentication");
                }

                var user = new User
                {
                    Username = newUsername,
                    Email = newEmail,
                    PhoneNumber = request.PhoneNumber ?? existingUser.PhoneNumber,
                    Name = request.Name ?? existingUser.Name,
                    Gender = request.Gender ?? existingUser.Gender,
                    DeviceToken = request.DeviceToken ?? existingUser.DeviceToken
                };

                var updatedUser = await _userService.UpdateUserAsync(userId, user);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("{userId}/device-token")]
        public async Task<ActionResult> RegisterDeviceToken(string userId, [FromBody] DeviceTokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.DeviceToken))
                {
                    return BadRequest("Device token is required");
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("userId is required");
                }

                // Check if user exists
                var existingUser = await _userRepository.GetUserByIdAsync(userId);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                var success = await _userService.RegisterDeviceTokenAsync(userId, request.DeviceToken);
                if (success)
                {
                    return Ok(new { message = "Device token registered successfully" });
                }
                else
                {
                    return BadRequest("Failed to register device token");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("{userId}/test-notification")]
        public async Task<ActionResult> TestNotification(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("userId is required");
                }

                // Check if user exists
                var existingUser = await _userRepository.GetUserByIdAsync(userId);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                var success = await _userService.TestNotificationAsync(userId);
                if (success)
                {
                    return Ok(new { message = "Test notification sent successfully" });
                }
                else
                {
                    return BadRequest("Failed to send test notification");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Send notification without authentication - only requires device token
        /// </summary>
        [HttpPost("send-notification")]
        [AllowAnonymous]
        public async Task<ActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.DeviceToken) || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Body))
                {
                    return BadRequest("DeviceToken, Title, and Body are required");
                }

                // Send notification directly using device token
                var success = await _notificationService.SendNotificationAsync(request.DeviceToken, request.Title, request.Body);
                if (success)
                {
                    return Ok(new { message = "Notification sent successfully" });
                }
                else
                {
                    return BadRequest("Failed to send notification");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

    }

    public class CreateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? DeviceToken { get; set; }
        public string? AbuneId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? DeviceToken { get; set; }
    }

    public class DeviceTokenRequest
    {
        public string? DeviceToken { get; set; }
    }

    public class SendNotificationRequest
    {
        public string? DeviceToken { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
    }

}
