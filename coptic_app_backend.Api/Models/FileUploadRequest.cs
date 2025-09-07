using Microsoft.AspNetCore.Http;

namespace coptic_app_backend.Api.Models
{
    /// <summary>
    /// Request model for file upload with chat message
    /// </summary>
    public class FileUploadRequest
    {
        /// <summary>
        /// Recipient user ID
        /// </summary>
        public string RecipientId { get; set; } = string.Empty;

        /// <summary>
        /// Message content (optional for file messages)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Type of message (0=Text, 1=Image, 2=Document, 3=Voice)
        /// </summary>
        public int MessageType { get; set; } = 0;

        /// <summary>
        /// File to upload (optional for text messages)
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// Duration in seconds for voice messages
        /// </summary>
        public int? VoiceDuration { get; set; }
    }
}

