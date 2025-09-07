using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    /// <summary>
    /// Service interface for chat operations
    /// </summary>
    public interface IChatService
    {
        // Core messaging
        Task<ChatMessage> SendMessageAsync(string senderId, string recipientId, string abuneId, string content, MessageType messageType = MessageType.Text);
        Task<ChatMessage> SendMediaMessageAsync(string senderId, string recipientId, string abuneId, string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null);
        Task<ChatMessage> SendBroadcastMessageAsync(string senderId, string abuneId, string content, MessageType messageType = MessageType.Text);
        Task<ChatMessage> SendBroadcastMediaMessageAsync(string senderId, string abuneId, string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null);
        
        // Message management
        Task<bool> UpdateMessageAsync(ChatMessage message);
        Task<ChatMessage?> EditMessageAsync(string messageId, string userId, string newContent);
        Task<bool> DeleteMessageAsync(string messageId, string userId);
        Task<bool> MarkMessageAsReadAsync(string messageId, string userId);
        Task<bool> AddReactionAsync(string messageId, string userId, string emoji);
        Task<bool> RemoveReactionAsync(string messageId, string userId);
        Task<ChatMessage> ReplyToMessageAsync(string senderId, string recipientId, string abuneId, string content, string replyToMessageId, MessageType messageType = MessageType.Text);
        Task<ChatMessage> ForwardMessageAsync(string senderId, string recipientId, string abuneId, string forwardFromMessageId);
        Task<ChatMessage?> GetMessageByIdAsync(string messageId);
        Task<ChatConversation?> GetConversationByIdAsync(string conversationId);
        Task<bool> UpdateConversationForMessageAsync(ChatMessage message);
        
        // Conversation management
        Task<List<ChatConversation>> GetUserConversationsAsync(string userId, string abuneId);
        Task<List<ChatMessage>> GetConversationMessagesAsync(string userId, string otherUserId, string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<ChatConversation> GetOrCreateConversationAsync(string abuneId, string userId, string communityAbuneId);
        
        // Read status and notifications
        Task<int> GetUnreadCountAsync(string userId, string conversationId);
        Task<Dictionary<string, int>> GetUnreadCountsForUserAsync(string userId, string abuneId);
        Task<List<MessageReadStatus>> GetMessageReadStatusAsync(string messageId);
        Task<bool> MarkConversationAsReadAsync(string conversationId, string userId);
        
        // Community features
        Task<List<ChatMessage>> GetCommunityMessagesAsync(string userId, string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<List<ChatMessage>> GetBroadcastMessagesAsync(string userId, string abuneId, int limit = 50, long? beforeTimestamp = null);
        Task<List<string>> GetCommunityMemberIdsAsync(string abuneId);
        
        // Search and discovery
        Task<List<ChatMessage>> SearchMessagesAsync(string userId, string abuneId, string searchTerm, int limit = 20);
        
        // Validation and permissions
        Task<bool> CanUserSendMessageToAsync(string senderId, string recipientId, string abuneId);
        Task<bool> CanUserAccessConversationAsync(string userId, string conversationId, string abuneId);
        Task<bool> IsUserInCommunityAsync(string userId, string abuneId);
    }
}
