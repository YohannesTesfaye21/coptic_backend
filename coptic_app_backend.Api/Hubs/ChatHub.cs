using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using coptic_app_backend.Domain.Models;
using System.Text.Json;

namespace coptic_app_backend.Api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat features
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, UserConnection> _userConnections = new();
        private static readonly Dictionary<string, HashSet<string>> _communityGroups = new();

        public class UserConnection
        {
            public string UserId { get; set; } = string.Empty;
            public string AbuneId { get; set; } = string.Empty;
            public string ConnectionId { get; set; } = string.Empty;
            public long LastSeen { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            public bool IsOnline { get; set; } = true;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(abuneId))
                {
                    Context.Abort();
                    return;
                }

                // Store user connection
                _userConnections[userId] = new UserConnection
                {
                    UserId = userId,
                    AbuneId = abuneId,
                    ConnectionId = Context.ConnectionId,
                    LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IsOnline = true
                };

                // Add to community group
                if (!_communityGroups.ContainsKey(abuneId))
                {
                    _communityGroups[abuneId] = new HashSet<string>();
                }
                _communityGroups[abuneId].Add(userId);

                await Groups.AddToGroupAsync(Context.ConnectionId, abuneId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

                // Notify community members about user coming online
                await Clients.Group(abuneId).SendAsync("UserOnline", userId);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                // Log error
                Context.Abort();
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetUserIdFromConnection(Context.ConnectionId);
                if (!string.IsNullOrEmpty(userId) && _userConnections.ContainsKey(userId))
                {
                    var userConnection = _userConnections[userId];
                    userConnection.IsOnline = false;
                    userConnection.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    // Remove from community group
                    if (_communityGroups.ContainsKey(userConnection.AbuneId))
                    {
                        _communityGroups[userConnection.AbuneId].Remove(userId);
                    }

                    // Notify community members about user going offline
                    await Clients.Group(userConnection.AbuneId).SendAsync("UserOffline", userId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        #region Real-time Messaging

        /// <summary>
        /// Send a message to a specific user
        /// </summary>
        public async Task SendMessage(string recipientId, string content, MessageType messageType, string? replyToMessageId = null)
        {
            try
            {
                var senderId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(abuneId))
                {
                    return;
                }

                var message = new ChatMessage
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    AbuneId = abuneId,
                    Content = content,
                    MessageType = messageType,
                    ReplyToMessageId = replyToMessageId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = MessageStatus.Sent
                };

                // Send to recipient
                await Clients.Group(recipientId).SendAsync("ReceiveMessage", message);

                // Send delivery confirmation to sender
                await Clients.Caller.SendAsync("MessageDelivered", message.Id, recipientId);

                // Update user's last seen
                UpdateUserLastSeen(senderId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Failed to send message");
            }
        }

        /// <summary>
        /// Send a media message to a specific user
        /// </summary>
        public async Task SendMediaMessage(string recipientId, string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null)
        {
            try
            {
                var senderId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(abuneId))
                {
                    return;
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
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = MessageStatus.Sent
                };

                // Send to recipient
                await Clients.Group(recipientId).SendAsync("ReceiveMediaMessage", message);

                // Send delivery confirmation to sender
                await Clients.Caller.SendAsync("MessageDelivered", message.Id, recipientId);

                // Update user's last seen
                UpdateUserLastSeen(senderId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Failed to send media message");
            }
        }

        /// <summary>
        /// Send a broadcast message to all community members
        /// </summary>
        public async Task SendBroadcastMessage(string content, MessageType messageType)
        {
            try
            {
                var senderId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(abuneId))
                {
                    return;
                }

                var message = new ChatMessage
                {
                    SenderId = senderId,
                    RecipientId = string.Empty,
                    AbuneId = abuneId,
                    Content = content,
                    MessageType = messageType,
                    IsBroadcast = true,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = MessageStatus.Sent
                };

                // Send to all community members
                await Clients.Group(abuneId).SendAsync("ReceiveBroadcastMessage", message);

                // Update user's last seen
                UpdateUserLastSeen(senderId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Failed to send broadcast message");
            }
        }

        /// <summary>
        /// Send a broadcast media message to all community members
        /// </summary>
        public async Task SendBroadcastMediaMessage(string fileUrl, string fileName, long fileSize, string fileType, MessageType messageType, int? voiceDuration = null)
        {
            try
            {
                var senderId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(abuneId))
                {
                    return;
                }

                var message = new ChatMessage
                {
                    SenderId = senderId,
                    RecipientId = string.Empty,
                    AbuneId = abuneId,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileType = fileType,
                    MessageType = messageType,
                    VoiceDuration = voiceDuration,
                    IsBroadcast = true,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = MessageStatus.Sent
                };

                // Send to all community members
                await Clients.Group(abuneId).SendAsync("ReceiveBroadcastMediaMessage", message);

                // Update user's last seen
                UpdateUserLastSeen(senderId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Failed to send broadcast media message");
            }
        }

        #endregion

        #region Typing Indicators

        /// <summary>
        /// Send typing indicator to a specific user
        /// </summary>
        public async Task SendTypingIndicator(string recipientId, bool isTyping)
        {
            try
            {
                var senderId = Context.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(senderId))
                {
                    return;
                }

                await Clients.Group(recipientId).SendAsync("TypingIndicator", senderId, isTyping);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        #endregion

        #region Message Status

        /// <summary>
        /// Mark a message as read
        /// </summary>
        public async Task MarkMessageAsRead(string messageId)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                // Notify sender that message was read
                await Clients.Caller.SendAsync("MessageRead", messageId, userId);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        /// <summary>
        /// Add reaction to a message
        /// </summary>
        public async Task AddReaction(string messageId, string emoji)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                // Notify all users in the conversation about the reaction
                await Clients.Caller.SendAsync("ReactionAdded", messageId, userId, emoji);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        #endregion

        #region User Status

        /// <summary>
        /// Get online users in the community
        /// </summary>
        public async Task GetOnlineUsers()
        {
            try
            {
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;
                if (string.IsNullOrEmpty(abuneId))
                {
                    return;
                }

                var onlineUsers = _userConnections.Values
                    .Where(uc => uc.AbuneId == abuneId && uc.IsOnline)
                    .Select(uc => uc.UserId)
                    .ToList();

                await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        /// <summary>
        /// Get unread count for current user
        /// </summary>
        public async Task GetUnreadCount()
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                var abuneId = Context.User?.FindFirst("AbuneId")?.Value;
                
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(abuneId))
                {
                    return;
                }

                // This would need to be injected via dependency injection
                // For now, we'll send a placeholder response
                await Clients.Caller.SendAsync("UnreadCount", new { count = 0 });
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        /// <summary>
        /// Update user's last seen timestamp
        /// </summary>
        public async Task UpdateLastSeen()
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId) && _userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        #endregion

        #region Unread Count Management

        /// <summary>
        /// Broadcast unread count update to a specific user
        /// </summary>
        public async Task BroadcastUnreadCountUpdate(string userId, int totalUnreadCount, Dictionary<string, int> conversationUnreadCounts)
        {
            try
            {
                await Clients.Group(userId).SendAsync("UnreadCountUpdate", new 
                { 
                    totalUnreadCount = totalUnreadCount,
                    conversationUnreadCounts = conversationUnreadCounts,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        /// <summary>
        /// Broadcast unread count update to all users in a community
        /// </summary>
        public async Task BroadcastUnreadCountUpdateToCommunity(string abuneId, Dictionary<string, object> userUnreadCounts)
        {
            try
            {
                await Clients.Group(abuneId).SendAsync("CommunityUnreadCountUpdate", new 
                { 
                    userUnreadCounts = userUnreadCounts,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        #endregion

        #region Private Methods

        private string? GetUserIdFromConnection(string connectionId)
        {
            return _userConnections.Values
                .FirstOrDefault(uc => uc.ConnectionId == connectionId)?.UserId;
        }

        private void UpdateUserLastSeen(string userId)
        {
            if (_userConnections.ContainsKey(userId))
            {
                _userConnections[userId].LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        #endregion
    }
}
