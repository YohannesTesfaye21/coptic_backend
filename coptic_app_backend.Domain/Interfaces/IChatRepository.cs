using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for chat operations
    /// </summary>
    public interface IChatRepository
    {
        // Message operations
        Task<ChatMessage> CreateMessageAsync(ChatMessage message);
        Task<ChatMessage?> GetMessageByIdAsync(string messageId);
        Task<List<ChatMessage>> GetMessagesByConversationAsync(string participant1Id, string participant2Id, string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<List<ChatMessage>> GetBroadcastMessagesAsync(string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<bool> UpdateMessageAsync(ChatMessage message);
        Task<bool> DeleteMessageAsync(string messageId, string deletedBy);
        Task<bool> MarkMessageAsReadAsync(string messageId, string userId);
        Task<bool> AddReactionAsync(string messageId, string userId, string emoji);
        Task<bool> RemoveReactionAsync(string messageId, string userId);
        
        // Conversation operations
        Task<ChatConversation> CreateConversationAsync(ChatConversation conversation);
        Task<ChatConversation?> GetConversationAsync(string abuneId, string userId, string communityAbuneId);
        Task<List<ChatConversation>> GetUserConversationsAsync(string userId, string abuneId);
        Task<bool> UpdateConversationAsync(ChatConversation conversation);
        Task<bool> DeactivateConversationAsync(string conversationId);
        
        // Read status and unread count
        Task<int> GetUnreadCountAsync(string userId, string conversationId);
        Task<Dictionary<string, int>> GetUnreadCountsForUserAsync(string userId, string abuneId);
        Task<List<MessageReadStatus>> GetMessageReadStatusAsync(string messageId);
        
        // Community messaging
        Task<List<ChatMessage>> GetCommunityMessagesAsync(string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<List<string>> GetCommunityMemberIdsAsync(string abuneId);
        
        // Message search
        Task<List<ChatMessage>> SearchMessagesAsync(string userId, string abuneId, string searchTerm, int limit = 20);
    }
}
