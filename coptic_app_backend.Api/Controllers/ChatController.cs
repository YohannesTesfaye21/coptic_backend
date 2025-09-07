using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Api.Hubs;
using coptic_app_backend.Api.Models;
using Microsoft.Extensions.Logging;
using coptic_app_backend.Infrastructure.Data;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// Chat controller for hierarchical Abune-User communication
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<ChatController> _logger;
        private readonly ApplicationDbContext _context;

        public ChatController(IChatService chatService, IUserRepository userRepository, IHubContext<ChatHub> hubContext, IFileStorageService fileStorageService, ILogger<ChatController> logger, ApplicationDbContext context)
        {
            _chatService = chatService;
            _userRepository = userRepository;
            _hubContext = hubContext;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _context = context;
        }

        #region Core Messaging

        /// <summary>
        /// Send a text message (Abune to User or User to Abune only)
        /// </summary>
        /// <param name="request">Message request</param>
        /// <returns>Sent message</returns>
        [HttpPost("send")]
        public async Task<ActionResult<ChatMessage>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var message = await _chatService.SendMessageAsync(
                    currentUserId, 
                    request.RecipientId, 
                    currentUserAbuneId, 
                    request.Content, 
                    request.MessageType
                );

                // Send real-time WebSocket notification to recipient
                await _hubContext.Clients.Group(request.RecipientId).SendAsync("ReceiveMessage", message);
                
                // Send delivery confirmation to sender
                await _hubContext.Clients.Group(currentUserId).SendAsync("MessageDelivered", message.Id, request.RecipientId);

                // Update unread counts and broadcast to recipient
                await UpdateAndBroadcastUnreadCounts(request.RecipientId, currentUserAbuneId);

                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Send a message with file upload (unified endpoint for text and files)
        /// </summary>
        /// <param name="request">File upload request containing message and file data</param>
        /// <returns>Sent message with file information</returns>
        [HttpPost("send-with-file")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<object>> SendMessageWithFile([FromForm] FileUploadRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                string messageContent = request.Content ?? string.Empty;
                string? fileUrl = null;
                string? fileName = null;
                long fileSize = 0;

                // Handle file upload if provided
                if (request.File != null && request.File.Length > 0)
                {
                    using var stream = request.File.OpenReadStream();
                    fileName = await _fileStorageService.UploadFileAsync(
                        stream,
                        request.File.FileName,
                        request.File.ContentType
                    );
                    fileUrl = await _fileStorageService.GetFileUrlAsync(fileName);
                    fileSize = request.File.Length;

                    // For file messages, use file name as content if no content provided
                    if (string.IsNullOrEmpty(request.Content))
                    {
                        messageContent = request.File.FileName;
                    }
                }

                // Convert MessageType from int to enum
                var messageType = (MessageType)request.MessageType;

                // Send message through chat service
                var message = await _chatService.SendMessageAsync(
                    currentUserId,
                    request.RecipientId,
                    currentUserAbuneId,
                    messageContent,
                    messageType
                );

                // Update message with file metadata if file was uploaded
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    message.FileUrl = fileUrl;
                    message.FileName = fileName;
                    message.FileSize = fileSize;
                    message.FileType = request.File?.ContentType;
                    message.VoiceDuration = request.VoiceDuration;
                    
                    // Update the message in database with file metadata
                    await _chatService.UpdateMessageAsync(message);
                }

                // Send real-time WebSocket notification to recipient
                await _hubContext.Clients.Group(request.RecipientId).SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    recipientId = message.RecipientId,
                    content = messageContent,
                    messageType = messageType,
                    timestamp = message.Timestamp,
                    fileUrl = message.FileUrl,
                    fileName = message.FileName,
                    fileSize = message.FileSize,
                    fileType = message.FileType,
                    voiceDuration = message.VoiceDuration
                });

                // Send delivery confirmation to sender
                await _hubContext.Clients.Group(currentUserId).SendAsync("MessageDelivered", message.Id, request.RecipientId);

                // Update unread counts and broadcast to recipient
                await UpdateAndBroadcastUnreadCounts(request.RecipientId, currentUserAbuneId);

                return Ok(new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    recipientId = message.RecipientId,
                    content = messageContent,
                    messageType = messageType,
                    timestamp = message.Timestamp,
                    fileUrl = message.FileUrl,
                    fileName = message.FileName,
                    fileSize = message.FileSize,
                    fileType = message.FileType,
                    voiceDuration = message.VoiceDuration
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message with file");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Send a media message (Abune to User or User to Abune only)
        /// </summary>
        /// <param name="request">Media message request</param>
        /// <returns>Sent message</returns>
        [HttpPost("send-media")]
        public async Task<ActionResult<ChatMessage>> SendMediaMessage([FromBody] SendMediaMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var message = await _chatService.SendMediaMessageAsync(
                    currentUserId,
                    request.RecipientId,
                    currentUserAbuneId,
                    request.FileUrl,
                    request.FileName,
                    request.FileSize,
                    request.FileType,
                    request.MessageType,
                    request.VoiceDuration
                );

                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Broadcast Messaging (Abune Only)

        /// <summary>
        /// Send a broadcast message to all community members (Abune only)
        /// </summary>
        /// <param name="request">Broadcast message request</param>
        /// <returns>Sent broadcast message</returns>
        [HttpPost("broadcast")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<ChatMessage>> SendBroadcastMessage([FromBody] SendBroadcastMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var message = await _chatService.SendBroadcastMessageAsync(
                    currentUserId,
                    currentUserAbuneId,
                    request.Content,
                    request.MessageType
                );

                // Send real-time WebSocket notification to all community members
                await _hubContext.Clients.Group(currentUserAbuneId).SendAsync("ReceiveBroadcastMessage", message);

                // Update unread counts for all community members
                await UpdateAndBroadcastUnreadCountsToCommunity(currentUserAbuneId);

                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Send a broadcast media message to all community members (Abune only)
        /// </summary>
        /// <param name="request">Broadcast media message request</param>
        /// <returns>Sent broadcast message</returns>
        [HttpPost("broadcast-media")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<ChatMessage>> SendBroadcastMediaMessage([FromBody] SendBroadcastMediaMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var message = await _chatService.SendBroadcastMediaMessageAsync(
                    currentUserId,
                    currentUserAbuneId,
                    request.FileUrl,
                    request.FileName,
                    request.FileSize,
                    request.FileType,
                    request.MessageType,
                    request.VoiceDuration
                );

                return Ok(message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Message Management


        /// <summary>
        /// Mark a message as read
        /// </summary>
        /// <param name="messageId">Message ID to mark as read</param>
        /// <returns>Mark as read result</returns>
        [HttpPost("{messageId}/read")]
        public async Task<ActionResult> MarkMessageAsRead(string messageId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                var success = await _chatService.MarkMessageAsReadAsync(messageId, currentUserId);
                if (success)
                {
                    return Ok(new { message = "Message marked as read" });
                }

                return BadRequest("Failed to mark message as read");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Add a reaction to a message
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <param name="request">Reaction request</param>
        /// <returns>Add reaction result</returns>
        [HttpPost("{messageId}/reactions")]
        public async Task<ActionResult> AddReaction(string messageId, [FromBody] AddReactionRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                var success = await _chatService.AddReactionAsync(messageId, currentUserId, request.Emoji);
                if (success)
                {
                    return Ok(new { message = "Reaction added successfully" });
                }

                return BadRequest("Failed to add reaction");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Remove a reaction from a message
        /// </summary>
        /// <param name="messageId">Message ID</param>
        /// <returns>Remove reaction result</returns>
        [HttpDelete("{messageId}/reactions")]
        public async Task<ActionResult> RemoveReaction(string messageId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                var success = await _chatService.RemoveReactionAsync(messageId, currentUserId);
                if (success)
                {
                    return Ok(new { message = "Reaction removed successfully" });
                }

                return BadRequest("Failed to remove reaction");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Messages

        /// <summary>
        /// Get messages for a specific conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>List of messages</returns>
        [HttpGet("messages")]
        public async Task<ActionResult<List<ChatMessage>>> GetMessages(string conversationId, int page = 1, int pageSize = 50)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var messages = await _chatService.GetMessagesAsync(conversationId, page, pageSize);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get messages for conversation {ConversationId}", conversationId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Conversations

        /// <summary>
        /// Get user's conversations with unread counts
        /// </summary>
        /// <returns>List of conversations with unread counts</returns>
        [HttpGet("conversations")]
        public async Task<ActionResult<object>> GetConversations()
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var conversations = await _chatService.GetUserConversationsAsync(currentUserId, currentUserAbuneId);
                var unreadCounts = await _chatService.GetUnreadCountsForUserAsync(currentUserId, currentUserAbuneId);
                
                // Calculate total unread count
                var totalUnreadCount = unreadCounts.Values.Sum();

                // Add unread counts to conversations
                var conversationsWithUnread = conversations.Select(conv => new
                {
                    id = conv.Id,
                    abuneId = conv.AbuneId,
                    userId = conv.UserId,
                    lastMessageAt = conv.LastMessageAt,
                    lastMessageContent = conv.LastMessageContent,
                    lastMessageType = conv.LastMessageType,
                    unreadCount = unreadCounts.ContainsKey(conv.Id) ? unreadCounts[conv.Id] : 0,
                    isActive = conv.IsActive,
                    createdAt = conv.CreatedAt,
                    updatedAt = conv.UpdatedAt,
                    abune = conv.Abune,
                    user = conv.User
                }).ToList();

                var result = new
                {
                    conversations = conversationsWithUnread,
                    totalUnreadCount = totalUnreadCount,
                    unreadCounts = unreadCounts
                };

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get messages from a specific conversation
        /// </summary>
        /// <param name="otherUserId">Other participant's user ID</param>
        /// <param name="limit">Number of messages to retrieve</param>
        /// <param name="beforeTimestamp">Get messages before this timestamp</param>
        /// <returns>List of messages</returns>
        [HttpGet("conversations/{otherUserId}/messages")]
        public async Task<ActionResult<List<ChatMessage>>> GetConversationMessages(string otherUserId, [FromQuery] int limit = 50, [FromQuery] long? beforeTimestamp = null)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var messages = await _chatService.GetConversationMessagesAsync(currentUserId, otherUserId, currentUserAbuneId, limit, beforeTimestamp);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Read Status and Unread Counts

        /// <summary>
        /// Get unread count for a specific conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>Unread count</returns>
        [HttpGet("conversations/{conversationId}/unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount(string conversationId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                var unreadCount = await _chatService.GetUnreadCountAsync(currentUserId, conversationId);
                return Ok(unreadCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get unread counts for all conversations
        /// </summary>
        /// <returns>Dictionary of conversation ID to unread count</returns>
        [HttpGet("unread-counts")]
        public async Task<ActionResult<Dictionary<string, int>>> GetUnreadCounts()
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var unreadCounts = await _chatService.GetUnreadCountsForUserAsync(currentUserId, currentUserAbuneId);
                return Ok(unreadCounts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Mark messages as read in a conversation
        /// </summary>
        /// <param name="conversationId">Conversation ID</param>
        /// <returns>Success status</returns>
        [HttpPost("conversations/{conversationId}/mark-read")]
        public async Task<ActionResult> MarkConversationAsRead(string conversationId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Mark conversation as read
                var success = await _chatService.MarkConversationAsReadAsync(conversationId, currentUserId);
                if (!success)
                {
                    return BadRequest("Failed to mark conversation as read");
                }

                // Update and broadcast unread counts
                await UpdateAndBroadcastUnreadCounts(currentUserId, currentUserAbuneId);

                return Ok(new { success = true, message = "Conversation marked as read" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Community and Broadcast Messages

        /// <summary>
        /// Get community messages (non-broadcast)
        /// </summary>
        /// <param name="limit">Number of messages to retrieve</param>
        /// <param name="beforeTimestamp">Get messages before this timestamp</param>
        /// <returns>List of community messages</returns>
        [HttpGet("community")]
        public async Task<ActionResult<List<ChatMessage>>> GetCommunityMessages([FromQuery] int limit = 50, [FromQuery] long? beforeTimestamp = null)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var messages = await _chatService.GetCommunityMessagesAsync(currentUserId, currentUserAbuneId, limit, beforeTimestamp);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get broadcast messages
        /// </summary>
        /// <param name="limit">Number of messages to retrieve</param>
        /// <param name="beforeTimestamp">Get messages before this timestamp</param>
        /// <returns>List of broadcast messages</returns>
        [HttpGet("broadcast")]
        public async Task<ActionResult<List<ChatMessage>>> GetBroadcastMessages([FromQuery] int limit = 50, [FromQuery] long? beforeTimestamp = null)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var messages = await _chatService.GetBroadcastMessagesAsync(currentUserId, currentUserAbuneId, limit, beforeTimestamp);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// Search messages in user's conversations
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="limit">Number of results to return</param>
        /// <returns>List of matching messages</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<ChatMessage>>> SearchMessages([FromQuery] string searchTerm, [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var messages = await _chatService.SearchMessagesAsync(currentUserId, currentUserAbuneId, searchTerm, limit);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Message Management

        /// <summary>
        /// Delete a message
        /// </summary>
        /// <param name="messageId">Message ID to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("messages/{messageId}")]
        public async Task<ActionResult> DeleteMessage(string messageId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Get the message to check ownership
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Check if user is the sender or has permission to delete
                if (message.SenderId != currentUserId)
                {
                    return Forbid("You can only delete your own messages");
                }

                // Delete the message
                var success = await _chatService.DeleteMessageAsync(messageId, currentUserId);
                if (!success)
                {
                    return StatusCode(500, "Failed to delete message");
                }

                // Notify all participants in the conversation about the deletion
                var conversation = await _chatService.GetConversationByIdAsync(message.ConversationId);
                if (conversation != null)
                {
                    var participants = new List<string> { conversation.UserId, conversation.AbuneId };
                    foreach (var participant in participants)
                    {
                        await _hubContext.Clients.Group(participant).SendAsync("MessageDeleted", new
                        {
                            messageId = messageId,
                            conversationId = message.ConversationId,
                            deletedBy = currentUserId,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                    }
                }

                return Ok(new { message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Edit a message
        /// </summary>
        /// <param name="messageId">Message ID to edit</param>
        /// <param name="request">Edit message request</param>
        /// <returns>Updated message</returns>
        [HttpPut("messages/{messageId}")]
        public async Task<ActionResult<object>> EditMessage(string messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Get the message to check ownership
                var message = await _chatService.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Check if user is the sender
                if (message.SenderId != currentUserId)
                {
                    return Forbid("You can only edit your own messages");
                }

                // Edit the message using the service
                var editedMessage = await _chatService.EditMessageAsync(messageId, currentUserId, request.Content);
                if (editedMessage == null)
                {
                    return StatusCode(500, "Failed to edit message");
                }

                // Notify all participants in the conversation about the edit
                var conversation = await _chatService.GetConversationByIdAsync(message.ConversationId);
                if (conversation != null)
                {
                    var participants = new List<string> { conversation.UserId, conversation.AbuneId };
                    foreach (var participant in participants)
                    {
                        await _hubContext.Clients.Group(participant).SendAsync("MessageEdited", new
                        {
                            id = editedMessage.Id,
                            senderId = editedMessage.SenderId,
                            recipientId = editedMessage.RecipientId,
                            content = editedMessage.Content,
                            messageType = editedMessage.MessageType,
                            timestamp = editedMessage.Timestamp,
                            fileUrl = editedMessage.FileUrl,
                            fileName = editedMessage.FileName,
                            fileSize = editedMessage.FileSize,
                            fileType = editedMessage.FileType,
                            voiceDuration = editedMessage.VoiceDuration,
                            editedBy = currentUserId,
                            editedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                    }
                }

                return Ok(new
                {
                    id = editedMessage.Id,
                    senderId = editedMessage.SenderId,
                    recipientId = editedMessage.RecipientId,
                    content = editedMessage.Content,
                    messageType = editedMessage.MessageType,
                    timestamp = editedMessage.Timestamp,
                    fileUrl = editedMessage.FileUrl,
                    fileName = editedMessage.FileName,
                    fileSize = editedMessage.FileSize,
                    fileType = editedMessage.FileType,
                    voiceDuration = editedMessage.VoiceDuration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit message");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Reply to a message
        /// </summary>
        /// <param name="request">Reply message request</param>
        /// <returns>Reply message</returns>
        [HttpPost("reply")]
        public async Task<ActionResult<object>> ReplyToMessage([FromBody] ReplyMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Get the original message to determine recipient
                var originalMessage = await _chatService.GetMessageByIdAsync(request.OriginalMessageId);
                if (originalMessage == null)
                {
                    return NotFound("Original message not found");
                }

                // Determine recipient (opposite of original sender)
                string recipientId = originalMessage.SenderId == currentUserId ? originalMessage.RecipientId : originalMessage.SenderId;

                // Create reply content with reference to original message
                string replyContent = $"Replying to: {originalMessage.Content}\n\n{request.Content}";

                // Send the reply message
                var replyMessage = await _chatService.SendMessageAsync(
                    currentUserId,
                    recipientId,
                    currentUserAbuneId,
                    replyContent,
                    MessageType.Text
                );

                // Update conversation with the reply
                await _chatService.UpdateConversationForMessageAsync(replyMessage);

                // Send real-time WebSocket notification to recipient
                await _hubContext.Clients.Group(recipientId).SendAsync("ReceiveMessage", new
                {
                    id = replyMessage.Id,
                    senderId = replyMessage.SenderId,
                    recipientId = replyMessage.RecipientId,
                    content = replyMessage.Content,
                    messageType = replyMessage.MessageType,
                    timestamp = replyMessage.Timestamp,
                    fileUrl = replyMessage.FileUrl,
                    fileName = replyMessage.FileName,
                    fileSize = replyMessage.FileSize,
                    fileType = replyMessage.FileType,
                    voiceDuration = replyMessage.VoiceDuration,
                    isReply = true,
                    originalMessageId = request.OriginalMessageId
                });

                // Send delivery confirmation to sender
                await _hubContext.Clients.Group(currentUserId).SendAsync("MessageDelivered", replyMessage.Id, recipientId);

                // Update unread counts and broadcast to recipient
                await UpdateAndBroadcastUnreadCounts(recipientId, currentUserAbuneId);

                return Ok(new
                {
                    id = replyMessage.Id,
                    senderId = replyMessage.SenderId,
                    recipientId = replyMessage.RecipientId,
                    content = replyMessage.Content,
                    messageType = replyMessage.MessageType,
                    timestamp = replyMessage.Timestamp,
                    fileUrl = replyMessage.FileUrl,
                    fileName = replyMessage.FileName,
                    fileSize = replyMessage.FileSize,
                    fileType = replyMessage.FileType,
                    voiceDuration = replyMessage.VoiceDuration,
                    isReply = true,
                    originalMessageId = request.OriginalMessageId
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = "Forbidden", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reply message");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Update and broadcast unread counts to a specific user
        /// </summary>
        private async Task UpdateAndBroadcastUnreadCounts(string userId, string abuneId)
        {
            try
            {
                var unreadCounts = await _chatService.GetUnreadCountsForUserAsync(userId, abuneId);
                var totalUnreadCount = unreadCounts.Values.Sum();

                await _hubContext.Clients.Group(userId).SendAsync("UnreadCountUpdate", new
                {
                    totalUnreadCount = totalUnreadCount,
                    conversationUnreadCounts = unreadCounts,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking message sending
                Console.WriteLine($"Error updating unread counts for user {userId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Update and broadcast unread counts to all community members
        /// </summary>
        private async Task UpdateAndBroadcastUnreadCountsToCommunity(string abuneId)
        {
            try
            {
                // Get all community members
                var communityMembers = await _chatService.GetCommunityMemberIdsAsync(abuneId);
                var userUnreadCounts = new Dictionary<string, object>();

                foreach (var memberId in communityMembers)
                {
                    try
                    {
                        var unreadCounts = await _chatService.GetUnreadCountsForUserAsync(memberId, abuneId);
                        var totalUnreadCount = unreadCounts.Values.Sum();

                        userUnreadCounts[memberId] = new
                        {
                            totalUnreadCount = totalUnreadCount,
                            conversationUnreadCounts = unreadCounts
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting unread counts for member {memberId}: {ex.Message}");
                    }
                }

                await _hubContext.Clients.Group(abuneId).SendAsync("CommunityUnreadCountUpdate", new
                {
                    userUnreadCounts = userUnreadCounts,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking message sending
                Console.WriteLine($"Error updating community unread counts for abune {abuneId}: {ex.Message}");
            }
        }

        #endregion
    }

    #region Request Models

    public class SendMessageRequest
    {
        public string RecipientId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
    }

    public class SendMediaMessageRequest
    {
        public string RecipientId { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public int? VoiceDuration { get; set; }
    }

    public class SendBroadcastMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
    }

    public class SendBroadcastMediaMessageRequest
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public int? VoiceDuration { get; set; }
    }

    public class AddReactionRequest
    {
        public string Emoji { get; set; } = string.Empty;
    }

    public class EditMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class ReplyMessageRequest
    {
        public string OriginalMessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

        #endregion

        #region Database Maintenance

        /// <summary>
        /// Fix database schema - add ConversationId column if missing
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("fix-database")]
        public async Task<ActionResult> FixDatabase()
        {
            try
            {
                // Execute SQL to add ConversationId column if it doesn't exist
                var sql = @"
                    DO $$ 
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns 
                            WHERE table_name = 'ChatMessages' 
                            AND column_name = 'ConversationId'
                        ) THEN
                            ALTER TABLE ""ChatMessages"" ADD COLUMN ""ConversationId"" character varying(450);
                            CREATE INDEX IF NOT EXISTS ""IX_ChatMessages_ConversationId"" ON ""ChatMessages"" (""ConversationId"");
                        END IF;
                    END $$;";

                await _context.Database.ExecuteSqlRawAsync(sql);
                
                return Ok(new { message = "Database schema fixed successfully - ConversationId column added" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database fix failed", message = ex.Message });
            }
        }

        #endregion
    }
}
