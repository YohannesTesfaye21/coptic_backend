using System.Text.Json;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace coptic_app_backend.Infrastructure.Repositories
{
    /// <summary>
    /// PostgreSQL implementation of chat repository
    /// </summary>
    public class PostgreSQLChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public PostgreSQLChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Message Operations

        public async Task<ChatMessage> CreateMessageAsync(ChatMessage message)
        {
            message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            message.Status = MessageStatus.Sent;
            
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            
            // Update conversation
            await UpdateConversationLastMessageAsync(message);
            
            return message;
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(string messageId)
        {
            return await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);
        }

        public async Task<List<ChatMessage>> GetMessagesByConversationAsync(string participant1Id, string participant2Id, string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            var query = _context.ChatMessages
                .Where(m => !m.IsDeleted && 
                           m.AbuneId == abuneId &&
                           ((m.SenderId == participant1Id && m.RecipientId == participant2Id) ||
                            (m.SenderId == participant2Id && m.RecipientId == participant1Id)));

            if (beforeTimestamp.HasValue)
            {
                query = query.Where(m => m.Timestamp < beforeTimestamp.Value);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetBroadcastMessagesAsync(string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            var query = _context.ChatMessages
                .Where(m => !m.IsDeleted && m.AbuneId == abuneId && m.IsBroadcast);

            if (beforeTimestamp.HasValue)
            {
                query = query.Where(m => m.Timestamp < beforeTimestamp.Value);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> UpdateMessageAsync(ChatMessage message)
        {
            var existingMessage = await _context.ChatMessages.FindAsync(message.Id);
            if (existingMessage == null) return false;

            existingMessage.Content = message.Content;
            existingMessage.FileUrl = message.FileUrl;
            existingMessage.FileName = message.FileName;
            existingMessage.FileSize = message.FileSize;
            existingMessage.FileType = message.FileType;
            existingMessage.VoiceDuration = message.VoiceDuration;
            existingMessage.Reactions = message.Reactions;
            existingMessage.Status = message.Status;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string deletedBy)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            message.IsDeleted = true;
            message.DeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            message.DeletedBy = deletedBy;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId, string userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            try
            {
                var readStatus = JsonSerializer.Deserialize<Dictionary<string, MessageReadStatus>>(message.ReadStatus) ?? new Dictionary<string, MessageReadStatus>();
                
                readStatus[userId] = new MessageReadStatus
                {
                    UserId = userId,
                    ReadAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                message.ReadStatus = JsonSerializer.Serialize(readStatus);
                message.Status = MessageStatus.Read;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddReactionAsync(string messageId, string userId, string emoji)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            try
            {
                var reactions = JsonSerializer.Deserialize<List<MessageReaction>>(message.Reactions) ?? new List<MessageReaction>();
                
                // Remove existing reaction from this user
                reactions.RemoveAll(r => r.UserId == userId);
                
                // Add new reaction
                reactions.Add(new MessageReaction
                {
                    UserId = userId,
                    Emoji = emoji,
                    ReactedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });

                message.Reactions = JsonSerializer.Serialize(reactions);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveReactionAsync(string messageId, string userId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            try
            {
                var reactions = JsonSerializer.Deserialize<List<MessageReaction>>(message.Reactions) ?? new List<MessageReaction>();
                reactions.RemoveAll(r => r.UserId == userId);
                message.Reactions = JsonSerializer.Serialize(reactions);
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Conversation Operations

        public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation)
        {
            conversation.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            conversation.UpdatedAt = conversation.CreatedAt;
            
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            
            return conversation;
        }

        public async Task<ChatConversation?> GetConversationAsync(string abuneId, string userId, string communityAbuneId)
        {
            return await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.AbuneId == abuneId && 
                                        c.UserId == userId &&
                                        c.AbuneId == communityAbuneId &&
                                        c.IsActive);
        }

        public async Task<List<ChatConversation>> GetUserConversationsAsync(string userId, string abuneId)
        {
            return await _context.ChatConversations
                .Where(c => c.AbuneId == abuneId && 
                           c.IsActive &&
                           c.UserId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateConversationAsync(ChatConversation conversation)
        {
            conversation.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            _context.ChatConversations.Update(conversation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateConversationAsync(string conversationId)
        {
            var conversation = await _context.ChatConversations.FindAsync(conversationId);
            if (conversation == null) return false;

            conversation.IsActive = false;
            conversation.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Read Status and Unread Count

        public async Task<int> GetUnreadCountAsync(string userId, string conversationId)
        {
            var conversation = await _context.ChatConversations.FindAsync(conversationId);
            if (conversation == null) return 0;

            return await _context.ChatMessages
                .CountAsync(m => !m.IsDeleted && 
                               m.RecipientId == userId &&
                               m.AbuneId == conversation.AbuneId &&
                               m.SenderId == conversation.AbuneId &&
                               !m.ReadStatus.Contains(userId));
        }

        public async Task<Dictionary<string, int>> GetUnreadCountsForUserAsync(string userId, string abuneId)
        {
            var conversations = await GetUserConversationsAsync(userId, abuneId);
            var result = new Dictionary<string, int>();

            foreach (var conversation in conversations)
            {
                var unreadCount = await GetUnreadCountAsync(userId, conversation.Id);
                result[conversation.Id] = unreadCount;
            }

            return result;
        }

        public async Task<List<MessageReadStatus>> GetMessageReadStatusAsync(string messageId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return new List<MessageReadStatus>();

            try
            {
                return JsonSerializer.Deserialize<List<MessageReadStatus>>(message.ReadStatus) ?? new List<MessageReadStatus>();
            }
            catch
            {
                return new List<MessageReadStatus>();
            }
        }

        #endregion

        #region Community Messaging

        public async Task<List<ChatMessage>> GetCommunityMessagesAsync(string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            var query = _context.ChatMessages
                .Where(m => !m.IsDeleted && m.AbuneId == abuneId && !m.IsBroadcast);

            if (beforeTimestamp.HasValue)
            {
                query = query.Where(m => m.Timestamp < beforeTimestamp.Value);
            }

            return await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<string>> GetCommunityMemberIdsAsync(string abuneId)
        {
            return await _context.Users
                .Where(u => u.AbuneId == abuneId && u.UserStatus == UserStatus.Active)
                .Select(u => u.Id!)
                .ToListAsync();
        }

        #endregion

        #region Message Search

        public async Task<List<ChatMessage>> SearchMessagesAsync(string userId, string abuneId, string searchTerm, int limit = 20)
        {
            return await _context.ChatMessages
                .Where(m => !m.IsDeleted && 
                           m.AbuneId == abuneId &&
                           (m.SenderId == userId || m.RecipientId == userId) &&
                           (m.Content != null && m.Content.Contains(searchTerm) ||
                            m.FileName != null && m.FileName.Contains(searchTerm)))
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        #endregion

        #region Private Methods

        private async Task UpdateConversationLastMessageAsync(ChatMessage message)
        {
            // Determine the Abune and User IDs for the conversation
            string abuneId, userId;
            
            if (message.SenderId == message.AbuneId)
            {
                // Abune is sending to a user
                abuneId = message.SenderId;
                userId = message.RecipientId;
            }
            else
            {
                // User is sending to Abune
                abuneId = message.AbuneId;
                userId = message.SenderId;
            }
            
            var conversation = await GetConversationAsync(abuneId, userId, message.AbuneId);
            if (conversation == null)
            {
                // Create new conversation
                conversation = new ChatConversation
                {
                    AbuneId = abuneId,
                    UserId = userId,
                    LastMessageAt = message.Timestamp,
                    LastMessageContent = message.Content,
                    LastMessageType = message.MessageType,
                    UnreadCount = 1,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await CreateConversationAsync(conversation);
            }
            else
            {
                // Update existing conversation
                conversation.LastMessageAt = message.Timestamp;
                conversation.LastMessageContent = message.Content;
                conversation.LastMessageType = message.MessageType;
                conversation.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                // Increment unread count if message is from Abune to User
                if (message.SenderId == message.AbuneId)
                {
                    conversation.UnreadCount++;
                }
                
                await UpdateConversationAsync(conversation);
            }
        }

        #endregion
    }
}
