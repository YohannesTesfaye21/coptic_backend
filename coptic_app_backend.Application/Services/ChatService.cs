using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Application.Services
{
    /// <summary>
    /// Chat service implementation for hierarchical Abune-User communication
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;

        public ChatService(IChatRepository chatRepository, IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
        }

        #region Core Messaging

        public async Task<ChatMessage> SendMessageAsync(string senderId, string recipientId, string abuneId, string content, MessageType messageType = MessageType.Text)
        {
            // Validate that this is Abune-User communication only
            if (!await CanUserSendMessageToAsync(senderId, recipientId, abuneId))
            {
                throw new UnauthorizedAccessException("User can only send messages to their Abune, and Abune can only send messages to their community members");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                AbuneId = abuneId,
                Content = content,
                MessageType = messageType,
                IsBroadcast = false,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        public async Task<ChatMessage> SendMediaMessageAsync(string senderId, string recipientId, string abuneId, string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null)
        {
            // Validate permissions
            if (!await CanUserSendMessageToAsync(senderId, recipientId, abuneId))
            {
                throw new UnauthorizedAccessException("User cannot send message to this recipient");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                AbuneId = abuneId,
                FileUrl = fileUrl,
                FileName = fileName,
                FileSize = fileSize,
                FileType = fileType,
                MessageType = messageType,
                VoiceDuration = voiceDuration,
                IsBroadcast = false,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        public async Task<ChatMessage> SendBroadcastMessageAsync(string senderId, string abuneId, string content, MessageType messageType = MessageType.Text)
        {
            // Validate that sender is an Abune
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            if (sender?.UserType != UserType.Abune || sender.AbuneId != abuneId)
            {
                throw new UnauthorizedAccessException("Only Abune users can send broadcast messages");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = string.Empty, // Broadcast messages don't have specific recipients
                AbuneId = abuneId,
                Content = content,
                MessageType = messageType,
                IsBroadcast = true,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        public async Task<ChatMessage> SendBroadcastMediaMessageAsync(string senderId, string abuneId, string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null)
        {
            // Validate that sender is an Abune
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            if (sender?.UserType != UserType.Abune || sender.AbuneId != abuneId)
            {
                throw new UnauthorizedAccessException("Only Abune users can send broadcast messages");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = string.Empty, // Broadcast messages don't have specific recipients
                AbuneId = abuneId,
                FileUrl = fileUrl,
                FileName = fileName,
                FileSize = fileSize,
                FileType = fileType,
                MessageType = messageType,
                VoiceDuration = voiceDuration,
                IsBroadcast = true,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        #endregion

        #region Message Management

        public async Task<bool> DeleteMessageAsync(string messageId, string userId)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            // Only sender can delete their own message
            if (message.SenderId != userId)
            {
                throw new UnauthorizedAccessException("User can only delete their own messages");
            }

            return await _chatRepository.DeleteMessageAsync(messageId, userId);
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId, string userId)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            // Only recipient can mark message as read
            if (message.RecipientId != userId)
            {
                throw new UnauthorizedAccessException("User can only mark messages sent to them as read");
            }

            return await _chatRepository.MarkMessageAsReadAsync(messageId, userId);
        }

        public async Task<bool> AddReactionAsync(string messageId, string userId, string emoji)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            // Validate that user is in the same community
            if (!await IsUserInCommunityAsync(userId, message.AbuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.AddReactionAsync(messageId, userId, emoji);
        }

        public async Task<bool> RemoveReactionAsync(string messageId, string userId)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            // Validate that user is in the same community
            if (!await IsUserInCommunityAsync(userId, message.AbuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.RemoveReactionAsync(messageId, userId);
        }

        public async Task<ChatMessage> ReplyToMessageAsync(string senderId, string recipientId, string abuneId, string content, string replyToMessageId, MessageType messageType = MessageType.Text)
        {
            // Validate permissions
            if (!await CanUserSendMessageToAsync(senderId, recipientId, abuneId))
            {
                throw new UnauthorizedAccessException("User cannot send message to this recipient");
            }

            // Validate reply message exists and is in the same conversation
            var replyToMessage = await _chatRepository.GetMessageByIdAsync(replyToMessageId);
            if (replyToMessage == null || replyToMessage.AbuneId != abuneId)
            {
                throw new ArgumentException("Reply message not found or invalid");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                AbuneId = abuneId,
                Content = content,
                MessageType = messageType,
                ReplyToMessageId = replyToMessageId,
                IsBroadcast = false,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        public async Task<ChatMessage> ForwardMessageAsync(string senderId, string recipientId, string abuneId, string forwardFromMessageId)
        {
            // Validate permissions
            if (!await CanUserSendMessageToAsync(senderId, recipientId, abuneId))
            {
                throw new UnauthorizedAccessException("User cannot send message to this recipient");
            }

            // Validate forward message exists and is in the same community
            var forwardFromMessage = await _chatRepository.GetMessageByIdAsync(forwardFromMessageId);
            if (forwardFromMessage == null || forwardFromMessage.AbuneId != abuneId)
            {
                throw new ArgumentException("Forward message not found or invalid");
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                RecipientId = recipientId,
                AbuneId = abuneId,
                Content = forwardFromMessage.Content,
                MessageType = forwardFromMessage.MessageType,
                FileUrl = forwardFromMessage.FileUrl,
                FileName = forwardFromMessage.FileName,
                FileSize = forwardFromMessage.FileSize,
                FileType = forwardFromMessage.FileType,
                VoiceDuration = forwardFromMessage.VoiceDuration,
                ForwardedFromMessageId = forwardFromMessageId,
                IsBroadcast = false,
                Status = MessageStatus.Sent
            };

            return await _chatRepository.CreateMessageAsync(message);
        }

        #endregion

        #region Conversation Management

        public async Task<List<ChatConversation>> GetUserConversationsAsync(string userId, string abuneId)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.GetUserConversationsAsync(userId, abuneId);
        }

        public async Task<List<ChatMessage>> GetConversationMessagesAsync(string userId, string otherUserId, string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId) || !await IsUserInCommunityAsync(otherUserId, abuneId))
            {
                throw new UnauthorizedAccessException("Users are not in the same community");
            }

            return await _chatRepository.GetMessagesByConversationAsync(userId, otherUserId, abuneId, limit, beforeTimestamp);
        }

        public async Task<ChatConversation> GetOrCreateConversationAsync(string abuneId, string userId, string communityAbuneId)
        {
            if (!await IsUserInCommunityAsync(abuneId, communityAbuneId) || !await IsUserInCommunityAsync(userId, communityAbuneId))
            {
                throw new UnauthorizedAccessException("Users are not in the same community");
            }

            var conversation = await _chatRepository.GetConversationAsync(abuneId, userId, communityAbuneId);
            if (conversation != null) return conversation;

            // Create new conversation
            conversation = new ChatConversation
            {
                AbuneId = abuneId,
                UserId = userId,
                LastMessageAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UnreadCount = 0,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return await _chatRepository.CreateConversationAsync(conversation);
        }

        #endregion

        #region Read Status and Notifications

        public async Task<int> GetUnreadCountAsync(string userId, string conversationId)
        {
            var conversation = await _chatRepository.GetConversationAsync(conversationId, userId, "");
            if (conversation == null) return 0;

            return await _chatRepository.GetUnreadCountAsync(userId, conversationId);
        }

        public async Task<Dictionary<string, int>> GetUnreadCountsForUserAsync(string userId, string abuneId)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.GetUnreadCountsForUserAsync(userId, abuneId);
        }

        public async Task<List<MessageReadStatus>> GetMessageReadStatusAsync(string messageId)
        {
            return await _chatRepository.GetMessageReadStatusAsync(messageId);
        }

        #endregion

        #region Community Features

        public async Task<List<ChatMessage>> GetCommunityMessagesAsync(string userId, string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.GetCommunityMessagesAsync(abuneId, limit, beforeTimestamp);
        }

        public async Task<List<ChatMessage>> GetBroadcastMessagesAsync(string userId, string abuneId, int limit = 50, long? beforeTimestamp = null)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.GetBroadcastMessagesAsync(abuneId, limit, beforeTimestamp);
        }

        #endregion

        #region Search and Discovery

        public async Task<List<ChatMessage>> SearchMessagesAsync(string userId, string abuneId, string searchTerm, int limit = 20)
        {
            if (!await IsUserInCommunityAsync(userId, abuneId))
            {
                throw new UnauthorizedAccessException("User is not in this community");
            }

            return await _chatRepository.SearchMessagesAsync(userId, abuneId, searchTerm, limit);
        }

        #endregion

        #region Validation and Permissions

        public async Task<bool> CanUserSendMessageToAsync(string senderId, string recipientId, string abuneId)
        {
            // Users can only send messages within their community
            if (!await IsUserInCommunityAsync(senderId, abuneId) || !await IsUserInCommunityAsync(recipientId, abuneId))
            {
                return false;
            }

            // Users cannot send messages to themselves
            if (senderId == recipientId)
            {
                return false;
            }

            // Get sender and recipient users to check their types
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            var recipient = await _userRepository.GetUserByIdAsync(recipientId);

            if (sender == null || recipient == null)
                return false;

            // Check if sender is Abune
            if (sender.UserType == UserType.Abune)
            {
                // Abune can send to any community member (Regular users only)
                return recipient.UserType == UserType.Regular;
            }

            // Regular user can only send to their Abune
            if (sender.UserType == UserType.Regular)
            {
                return recipient.UserType == UserType.Abune && recipient.Id == sender.AbuneId;
            }

            return false;
        }

        public async Task<bool> CanUserAccessConversationAsync(string userId, string conversationId, string abuneId)
        {
            var conversation = await _chatRepository.GetConversationAsync(conversationId, userId, abuneId);
            return conversation != null && conversation.IsActive;
        }

        public async Task<bool> IsUserInCommunityAsync(string userId, string abuneId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            // Abune users are always in their own community
            if (user.UserType == UserType.Abune && user.Id == abuneId)
            {
                return true;
            }

            // Regular users must be approved and belong to the specified Abune
            return user.UserType == UserType.Regular && 
                   user.AbuneId == abuneId && 
                   user.IsApproved && 
                   user.UserStatus == UserStatus.Active;
        }

        #endregion
    }
}
