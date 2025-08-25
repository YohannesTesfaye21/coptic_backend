using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    /// <summary>
    /// File storage service interface for chat media files
    /// </summary>
    public interface IFileStorageService
    {
        #region Core File Operations
        
        /// <summary>
        /// Store a file for a chat message with proper organization
        /// </summary>
        Task<string> StoreFileAsync(Stream fileStream, string fileName, string contentType, string senderId, string recipientId, string abuneId, MessageType messageType);
        
        /// <summary>
        /// Store a broadcast file (sent to all community members)
        /// </summary>
        Task<string> StoreBroadcastFileAsync(Stream fileStream, string fileName, string contentType, string senderId, string abuneId, MessageType messageType);
        
        /// <summary>
        /// Get file stream for reading
        /// </summary>
        Task<Stream> GetFileAsync(string fileUrl);
        
        /// <summary>
        /// Delete a file
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrl);
        
        /// <summary>
        /// Get file information
        /// </summary>
        Task<FileInfo> GetFileInfoAsync(string fileUrl);
        
        #endregion
        
        #region Chat-Specific File Operations
        
        /// <summary>
        /// Get all files for a specific chat conversation
        /// </summary>
        Task<List<ChatFileInfo>> GetChatFilesAsync(string senderId, string recipientId, string abuneId, MessageType? messageType = null);
        
        /// <summary>
        /// Get all broadcast files for a community
        /// </summary>
        Task<List<ChatFileInfo>> GetBroadcastFilesAsync(string abuneId, MessageType? messageType = null);
        
        /// <summary>
        /// Clean up old files (older than specified days)
        /// </summary>
        Task<int> CleanupOldFilesAsync(int daysToKeep);
        
        #endregion
    }
    
    /// <summary>
    /// Chat file information model
    /// </summary>
    public class ChatFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string AbuneId { get; set; } = string.Empty;
        public bool IsBroadcast { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
    }
}
