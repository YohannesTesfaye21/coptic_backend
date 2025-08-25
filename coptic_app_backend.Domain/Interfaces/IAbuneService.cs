using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IAbuneService
    {
        // Abune management
        Task<User> CreateAbuneAsync(User abuneUser);
        Task<User?> GetAbuneByIdAsync(string abuneId);
        Task<User?> GetAbuneByEmailAsync(string email);
        Task<List<User>> GetAllAbunesAsync();
        Task<User> UpdateAbuneAsync(string abuneId, User abuneUser);
        Task<bool> DeleteAbuneAsync(string abuneId);
        
        // Community management
        Task<List<User>> GetCommunityMembersAsync(string abuneId);
        Task<User> AddCommunityMemberAsync(string abuneId, User regularUser);
        Task<bool> RemoveCommunityMemberAsync(string abuneId, string memberId);
        Task<bool> ApproveCommunityMemberAsync(string abuneId, string memberId);
        Task<bool> SuspendCommunityMemberAsync(string abuneId, string memberId);
        
        // User approval workflow
        Task<List<User>> GetPendingApprovalsAsync(string abuneId);
        Task<bool> ApproveUserAsync(string abuneId, string userId);
        Task<bool> RejectUserAsync(string abuneId, string userId, string reason);
        
        // Statistics
        Task<int> GetCommunitySizeAsync(string abuneId);
        Task<int> GetPendingApprovalsCountAsync(string abuneId);
    }
}
