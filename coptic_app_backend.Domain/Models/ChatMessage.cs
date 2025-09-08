using System.Text.Json;

namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// Chat message model for hierarchical Abune-User communication
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Sender's user ID (can be Abune or Regular user)
        /// </summary>
        public string SenderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Recipient's user ID (can be Abune or Regular user)
        /// </summary>
        public string RecipientId { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune ID for community-based messaging
        /// </summary>
        public string AbuneId { get; set; } = string.Empty;
        
        
        /// <summary>
        /// Message content (text message)
        /// </summary>
        public string? Content { get; set; }
        
        /// <summary>
        /// Message type (Text, Image, Document, Voice)
        /// </summary>
        public MessageType MessageType { get; set; } = MessageType.Text;
        
        /// <summary>
        /// File URL for media messages
        /// </summary>
        public string? FileUrl { get; set; }
        
        /// <summary>
        /// File name for media messages
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSize { get; set; }
        
        /// <summary>
        /// File MIME type
        /// </summary>
        public string? FileType { get; set; }
        
        /// <summary>
        /// Voice message duration in seconds
        /// </summary>
        public int? VoiceDuration { get; set; }
        
        /// <summary>
        /// Message timestamp (Unix timestamp)
        /// </summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        /// <summary>
        /// Whether this is a broadcast message to all community members
        /// </summary>
        public bool IsBroadcast { get; set; } = false;
        
        /// <summary>
        /// Reply to message ID (for threaded conversations)
        /// </summary>
        public string? ReplyToMessageId { get; set; }
        
        /// <summary>
        /// Forwarded from message ID
        /// </summary>
        public string? ForwardedFromMessageId { get; set; }
        
        /// <summary>
        /// Message reactions (JSON string)
        /// </summary>
        public string Reactions { get; set; } = "{}";
        
        /// <summary>
        /// Read status for each recipient (JSON string)
        /// </summary>
        public string ReadStatus { get; set; } = "{}";
        
        /// <summary>
        /// Message status (Sent, Delivered, Read, Failed)
        /// </summary>
        public MessageStatus Status { get; set; } = MessageStatus.Sent;
        
        /// <summary>
        /// Whether message is deleted
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        /// <summary>
        /// Deletion timestamp
        /// </summary>
        public long? DeletedAt { get; set; }
        
        /// <summary>
        /// User who deleted the message
        /// </summary>
        public string? DeletedBy { get; set; }
        
        /// <summary>
        /// Whether message was edited
        /// </summary>
        public bool IsEdited { get; set; } = false;
        
        /// <summary>
        /// Edit timestamp
        /// </summary>
        public long? EditedAt { get; set; }
        
        /// <summary>
        /// User who edited the message
        /// </summary>
        public string? EditedBy { get; set; }
        
        /// <summary>
        /// Navigation properties for Entity Framework
        /// </summary>
        public User? Sender { get; set; }
        public User? Recipient { get; set; }
        public User? Abune { get; set; }
        public ChatMessage? ReplyToMessage { get; set; }
        public ChatMessage? ForwardedFromMessage { get; set; }
    }

    /// <summary>
    /// Message types supported by the chat system
    /// </summary>
    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Document = 2,
        Voice = 3
    }

    /// <summary>
    /// Message status for delivery tracking
    /// </summary>
    public enum MessageStatus
    {
        Sent = 0,
        Delivered = 1,
        Read = 2,
        Failed = 3
    }

    /// <summary>
    /// Read status model for tracking message read status
    /// </summary>
    public class MessageReadStatus
    {
        public string UserId { get; set; } = string.Empty;
        public long ReadAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Message reaction model
    /// </summary>
    public class MessageReaction
    {
        public string UserId { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public long ReactedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

            /// <summary>
        /// Chat conversation model for Abune-User communication
        /// </summary>
        public class ChatConversation
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string AbuneId { get; set; } = string.Empty; // The Abune in the conversation
            public string UserId { get; set; } = string.Empty;  // The Regular user in the conversation
            public long LastMessageAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            public string? LastMessageContent { get; set; }
            public MessageType LastMessageType { get; set; } = MessageType.Text;
            public int UnreadCount { get; set; } = 0;
            public bool IsActive { get; set; } = true;
            public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Navigation properties
            public User? Abune { get; set; }
            public User? User { get; set; }
        }
}
