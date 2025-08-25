using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Infrastructure.Services
{
    public class AbuneService : IAbuneService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AbuneService> _logger;

        public AbuneService(ApplicationDbContext context, ILogger<AbuneService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> CreateAbuneAsync(User abuneUser)
        {
            try
            {
                // Validate Abune user
                if (string.IsNullOrEmpty(abuneUser.ChurchName))
                {
                    throw new ArgumentException("Church name is required for Abune users");
                }

                // Set Abune-specific properties
                abuneUser.UserType = UserType.Abune;
                abuneUser.UserStatus = UserStatus.Active;
                abuneUser.IsApproved = true;
                abuneUser.AbuneId = null; // Abune users don't belong to other Abunes
                abuneUser.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                abuneUser.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                _context.Users.Add(abuneUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Abune created successfully: {AbuneId} for church: {ChurchName}", 
                    abuneUser.Id, abuneUser.ChurchName);
                
                return abuneUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Abune user");
                throw;
            }
        }

        public async Task<User?> GetAbuneByIdAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == abuneId && u.UserType == UserType.Abune);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Abune by ID: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<User?> GetAbuneByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.UserType == UserType.Abune);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Abune by email: {Email}", email);
                throw;
            }
        }

        public async Task<List<User>> GetAllAbunesAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.UserType == UserType.Abune)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all Abunes");
                throw;
            }
        }

        public async Task<User> UpdateAbuneAsync(string abuneId, User abuneUser)
        {
            try
            {
                var existingAbune = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == abuneId && u.UserType == UserType.Abune);
                
                if (existingAbune == null)
                {
                    throw new ArgumentException($"Abune with ID {abuneId} not found");
                }

                // Update Abune-specific properties
                existingAbune.ChurchName = abuneUser.ChurchName ?? existingAbune.ChurchName;
                existingAbune.Location = abuneUser.Location ?? existingAbune.Location;
                existingAbune.ProfileImageUrl = abuneUser.ProfileImageUrl ?? existingAbune.ProfileImageUrl;
                existingAbune.Bio = abuneUser.Bio ?? existingAbune.Bio;
                existingAbune.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Abune updated successfully: {AbuneId}", abuneId);
                return existingAbune;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<bool> DeleteAbuneAsync(string abuneId)
        {
            try
            {
                var abune = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == abuneId && u.UserType == UserType.Abune);
                
                if (abune == null)
                {
                    return false;
                }

                // Check if Abune has community members by querying directly
                var hasCommunityMembers = await _context.Users
                    .AnyAsync(u => u.AbuneId == abuneId && u.UserType == UserType.Regular);
                
                if (hasCommunityMembers)
                {
                    throw new InvalidOperationException("Cannot delete Abune with existing community members");
                }

                _context.Users.Remove(abune);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Abune deleted successfully: {AbuneId}", abuneId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<List<User>> GetCommunityMembersAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.AbuneId == abuneId && u.UserType == UserType.Regular)
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community members for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<User> AddCommunityMemberAsync(string abuneId, User regularUser)
        {
            try
            {
                // Verify Abune exists
                var abune = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == abuneId && u.UserType == UserType.Abune);
                
                if (abune == null)
                {
                    throw new ArgumentException($"Abune with ID {abuneId} not found");
                }

                // Set Regular user properties
                regularUser.UserType = UserType.Regular;
                regularUser.UserStatus = UserStatus.PendingApproval;
                regularUser.AbuneId = abuneId;
                regularUser.IsApproved = false;
                regularUser.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                regularUser.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                _context.Users.Add(regularUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Community member added: {UserId} under Abune: {AbuneId}", 
                    regularUser.Id, abuneId);
                
                return regularUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding community member to Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<bool> RemoveCommunityMemberAsync(string abuneId, string memberId)
        {
            try
            {
                var member = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == memberId && u.AbuneId == abuneId);
                
                if (member == null)
                {
                    return false;
                }

                _context.Users.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Community member removed: {MemberId} from Abune: {AbuneId}", 
                    memberId, abuneId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing community member: {MemberId} from Abune: {AbuneId}", 
                    memberId, abuneId);
                throw;
            }
        }

        public async Task<bool> ApproveCommunityMemberAsync(string abuneId, string memberId)
        {
            try
            {
                var member = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == memberId && u.AbuneId == abuneId);
                
                if (member == null)
                {
                    return false;
                }

                member.UserStatus = UserStatus.Active;
                member.IsApproved = true;
                member.ApprovedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                member.ApprovedBy = abuneId;
                member.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Community member approved: {MemberId} by Abune: {AbuneId}", 
                    memberId, abuneId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving community member: {MemberId} by Abune: {AbuneId}", 
                    memberId, abuneId);
                throw;
            }
        }

        public async Task<bool> SuspendCommunityMemberAsync(string abuneId, string memberId)
        {
            try
            {
                var member = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == memberId && u.AbuneId == abuneId);
                
                if (member == null)
                {
                    return false;
                }

                member.UserStatus = UserStatus.Suspended;
                member.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Community member suspended: {MemberId} by Abune: {AbuneId}", 
                    memberId, abuneId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending community member: {MemberId} by Abune: {AbuneId}", 
                    memberId, abuneId);
                throw;
            }
        }

        public async Task<List<User>> GetPendingApprovalsAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.AbuneId == abuneId && 
                               u.UserType == UserType.Regular && 
                               u.UserStatus == UserStatus.PendingApproval)
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<bool> ApproveUserAsync(string abuneId, string userId)
        {
            return await ApproveCommunityMemberAsync(abuneId, userId);
        }

        public async Task<bool> RejectUserAsync(string abuneId, string userId, string reason)
        {
            try
            {
                var member = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.AbuneId == abuneId);
                
                if (member == null)
                {
                    return false;
                }

                member.UserStatus = UserStatus.Inactive;
                member.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("User rejected: {UserId} by Abune: {AbuneId}, Reason: {Reason}", 
                    userId, abuneId, reason);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId} by Abune: {AbuneId}", userId, abuneId);
                throw;
            }
        }

        public async Task<int> GetCommunitySizeAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .CountAsync(u => u.AbuneId == abuneId && u.UserType == UserType.Regular);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting community size for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<int> GetPendingApprovalsCountAsync(string abuneId)
        {
            try
            {
                return await _context.Users
                    .CountAsync(u => u.AbuneId == abuneId && 
                                   u.UserType == UserType.Regular && 
                                   u.UserStatus == UserStatus.PendingApproval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals count for Abune: {AbuneId}", abuneId);
                throw;
            }
        }
    }
}
