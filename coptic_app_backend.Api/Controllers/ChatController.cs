using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;

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

        public ChatController(IChatService chatService, IUserRepository userRepository)
        {
            _chatService = chatService;
            _userRepository = userRepository;
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
        /// Delete a message (only sender can delete)
        /// </summary>
        /// <param name="messageId">Message ID to delete</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{messageId}")]
        public async Task<ActionResult> DeleteMessage(string messageId)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                var success = await _chatService.DeleteMessageAsync(messageId, currentUserId);
                if (success)
                {
                    return Ok(new { message = "Message deleted successfully" });
                }

                return BadRequest("Failed to delete message");
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

        #region Conversations

        /// <summary>
        /// Get user's conversations
        /// </summary>
        /// <returns>List of conversations</returns>
        [HttpGet("conversations")]
        public async Task<ActionResult<List<ChatConversation>>> GetConversations()
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
                return Ok(conversations);
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

    #endregion
}
