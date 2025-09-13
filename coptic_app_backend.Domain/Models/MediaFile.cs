using System.Text.Json.Serialization;

namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// Media file model for tracking uploaded files in the database
    /// </summary>
    public class MediaFile
    {
        /// <summary>
        /// Unique file identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Original file name
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Stored object name (path in storage)
        /// </summary>
        public string ObjectName { get; set; } = string.Empty;
        
        /// <summary>
        /// File URL for access
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// File MIME type
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Media type (Book, Video, Audio, Other)
        /// </summary>
        public MediaType MediaType { get; set; }
        
        /// <summary>
        /// Folder ID where this file is stored
        /// </summary>
        public string FolderId { get; set; } = string.Empty;
        
        /// <summary>
        /// User ID who uploaded this file
        /// </summary>
        public string UploadedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune ID for community-based organization
        /// </summary>
        public string AbuneId { get; set; } = string.Empty;
        
        /// <summary>
        /// File description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Upload timestamp (Unix timestamp)
        /// </summary>
        public long UploadedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        /// <summary>
        /// Last modified timestamp (Unix timestamp)
        /// </summary>
        public long LastModified { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        /// <summary>
        /// Whether the file is active/visible
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Storage type (MinIO, Local, etc.)
        /// </summary>
        public string StorageType { get; set; } = "Local";
        
        /// <summary>
        /// Navigation properties for Entity Framework
        /// </summary>
        [JsonIgnore]
        public Folder? Folder { get; set; }
        [JsonIgnore]
        public User? UploadedByUser { get; set; }
        [JsonIgnore]
        public User? Abune { get; set; }
    }
}
