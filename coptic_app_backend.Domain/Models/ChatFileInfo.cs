using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// File information model for chat messages
    /// </summary>
    public class ChatFileInfo
    {
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// File URL for access
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// File type/extension
        /// </summary>
        public string FileType { get; set; } = string.Empty;
        
        /// <summary>
        /// Message type this file belongs to
        /// </summary>
        public MessageType MessageType { get; set; } = MessageType.Text;
        
        /// <summary>
        /// Sender ID
        /// </summary>
        public string SenderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Recipient ID (empty for broadcast messages)
        /// </summary>
        public string RecipientId { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune ID
        /// </summary>
        public string AbuneId { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this is a broadcast file
        /// </summary>
        public bool IsBroadcast { get; set; } = false;
        
        /// <summary>
        /// File creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// File last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}
