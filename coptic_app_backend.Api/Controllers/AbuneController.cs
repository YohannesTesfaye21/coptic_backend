using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Api.Attributes;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// Abune management controller for spiritual leaders and community management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all Abune operations
    public class AbuneController : ControllerBase
    {
        private readonly IAbuneService _abuneService;
        private readonly IUserService _userService;
        private readonly ICognitoUserService _cognitoUserService;
        private readonly ILogger<AbuneController> _logger;

        public AbuneController(IAbuneService abuneService, IUserService userService, ICognitoUserService cognitoUserService, ILogger<AbuneController> logger)
        {
            _abuneService = abuneService;
            _userService = userService;
            _cognitoUserService = cognitoUserService;
            _logger = logger;
        }

        // GET: api/Abune
        [HttpGet]
        [AbuneAccess]
        public async Task<ActionResult<List<User>>> GetAllAbunes()
        {
            try
            {
                // Only return the current Abune's information
                var currentAbuneId = HttpContext.Items["AbuneId"] as string;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in context");
                }

                var currentAbune = await _abuneService.GetAbuneByIdAsync(currentAbuneId);
                if (currentAbune == null)
                {
                    return NotFound("Abune not found");
                }

                return Ok(new List<User> { currentAbune });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Abune");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // GET: api/Abune/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<User>> GetAbune(string id)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only see their own profile
                if (id != currentAbuneId)
                {
                    return Forbid("You can only view your own profile");
                }

                var abune = await _abuneService.GetAbuneByIdAsync(id);
                if (abune == null)
                {
                    return NotFound($"Abune with ID {id} not found");
                }

                return Ok(abune);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new Abune user (admin operation)
        /// </summary>
        /// <param name="request">Abune creation information</param>
        /// <returns>Created Abune user</returns>
        /// <response code="201">Abune created successfully</response>
        /// <response code="400">Invalid Abune data</response>
        /// <response code="500">Internal server error</response>
        // POST: api/Abune
        [HttpPost]
        [AllowAnonymous] // Allow Abune creation without authentication (admin operation)
        public async Task<ActionResult<User>> CreateAbune([FromBody] CreateAbuneRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Abune data is required");
                }

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest("Email is required");
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Password is required");
                }

                if (string.IsNullOrEmpty(request.ChurchName))
                {
                    return BadRequest("Church name is required");
                }

                // Use the authentication service to create Abune with password
                var authResult = await _cognitoUserService.RegisterAbuneAsync(
                    request.Email, 
                    request.Password, 
                    request.Name, 
                    request.ChurchName, 
                    request.Location, 
                    request.Bio
                );

                if (!authResult.IsSuccess)
                {
                    return BadRequest(new { error = "Abune creation failed", message = authResult.ErrorMessage });
                }

                // Get the created Abune user
                var createdAbune = await _abuneService.GetAbuneByIdAsync(authResult.UserId!);

                return CreatedAtAction(nameof(GetAbune), new { id = createdAbune!.Id }, createdAbune);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Abune");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // PUT: api/Abune/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<User>> UpdateAbune(string id, [FromBody] UpdateAbuneRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Abune data is required");
                }

                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only update their own profile
                if (id != currentAbuneId)
                {
                    return Forbid("You can only update your own profile");
                }

                var abuneUser = new User
                {
                    ChurchName = request.ChurchName,
                    Location = request.Location,
                    Bio = request.Bio,
                    ProfileImageUrl = request.ProfileImageUrl
                };

                var updatedAbune = await _abuneService.UpdateAbuneAsync(id, abuneUser);
                return Ok(updatedAbune);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // DELETE: api/Abune/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> DeleteAbune(string id)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only delete their own profile
                if (id != currentAbuneId)
                {
                    return Forbid("You can only delete your own profile");
                }

                var result = await _abuneService.DeleteAbuneAsync(id);
                if (!result)
                {
                    return NotFound($"Abune with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // GET: api/Abune/{id}/community
        [HttpGet("{id}/community")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<List<User>>> GetCommunityMembers(string id)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only see their own community members
                if (id != currentAbuneId)
                {
                    return Forbid("You can only view your own community members");
                }

                var members = await _abuneService.GetCommunityMembersAsync(id);
                return Ok(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community members for Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // GET: api/Abune/{id}/pending-approvals
        [HttpGet("{id}/pending-approvals")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<List<User>>> GetPendingApprovals(string id)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only see their own pending approvals
                if (id != currentAbuneId)
                {
                    return Forbid("You can only view your own pending approvals");
                }

                var pendingUsers = await _abuneService.GetPendingApprovalsAsync(id);
                return Ok(pendingUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals for Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // POST: api/Abune/{id}/approve/{userId}
        [HttpPost("{id}/approve/{userId}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> ApproveUser(string id, string userId)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only approve users in their own community
                if (id != currentAbuneId)
                {
                    return Forbid("You can only approve users in your own community");
                }

                var result = await _abuneService.ApproveUserAsync(id, userId);
                if (!result)
                {
                    return NotFound($"User {userId} not found in community of Abune {id}");
                }

                return Ok(new { message = "User approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user: {UserId} by Abune: {Id}", userId, id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // POST: api/Abune/{id}/reject/{userId}
        [HttpPost("{id}/reject/{userId}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> RejectUser(string id, string userId, [FromBody] RejectUserRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Reason))
                {
                    return BadRequest("Rejection reason is required");
                }

                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only reject users in their own community
                if (id != currentAbuneId)
                {
                    return Forbid("You can only reject users in your own community");
                }

                var result = await _abuneService.RejectUserAsync(id, userId, request.Reason);
                if (!result)
                {
                    return NotFound($"User {userId} not found in community of Abune {id}");
                }

                return Ok(new { message = "User rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId} by Abune: {Id}", userId, id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // POST: api/Abune/{id}/suspend/{userId}
        [HttpPost("{id}/suspend/{userId}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> SuspendUser(string id, string userId)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only suspend users in their own community
                if (id != currentAbuneId)
                {
                    return Forbid("You can only suspend users in your own community");
                }

                var result = await _abuneService.SuspendCommunityMemberAsync(id, userId);
                if (!result)
                {
                    return NotFound($"User {userId} not found in community of Abune {id}");
                }

                return Ok(new { message = "User suspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user: {UserId} by Abune: {Id}", userId, id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        // GET: api/Abune/{id}/stats
        [HttpGet("{id}/stats")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<AbuneStats>> GetAbuneStats(string id)
        {
            try
            {
                // Get current Abune ID from JWT claims
                var currentAbuneId = User.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(currentAbuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                // Abune can only see their own stats
                if (id != currentAbuneId)
                {
                    return Forbid("You can only view your own stats");
                }

                var communitySize = await _abuneService.GetCommunitySizeAsync(id);
                var pendingApprovals = await _abuneService.GetPendingApprovalsCountAsync(id);

                var stats = new AbuneStats
                {
                    CommunitySize = communitySize,
                    PendingApprovals = pendingApprovals
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for Abune: {Id}", id);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }

    // Request/Response models
    /// <summary>
    /// Abune creation request model
    /// </summary>
    public class CreateAbuneRequest
    {
        /// <summary>
        /// Abune's email address (used as username)
        /// </summary>
        /// <example>abune@church.com</example>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune's password (minimum 8 characters)
        /// </summary>
        /// <example>abune123</example>
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune's display name
        /// </summary>
        /// <example>Father Michael</example>
        public string? Name { get; set; }
        
        /// <summary>
        /// Name of the church/community
        /// </summary>
        /// <example>St. Mary Coptic Church</example>
        public string ChurchName { get; set; } = string.Empty;
        
        /// <summary>
        /// Geographic location
        /// </summary>
        /// <example>Cairo, Egypt</example>
        public string? Location { get; set; }
        
        /// <summary>
        /// Abune's biography/description
        /// </summary>
        /// <example>Spiritual leader of our community</example>
        public string? Bio { get; set; }
        
        /// <summary>
        /// URL to profile image
        /// </summary>
        /// <example>https://example.com/profile.jpg</example>
        public string? ProfileImageUrl { get; set; }
    }

    public class UpdateAbuneRequest
    {
        public string? ChurchName { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class RejectUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AbuneStats
    {
        public int CommunitySize { get; set; }
        public int PendingApprovals { get; set; }
    }
}
