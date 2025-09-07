using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace coptic_app_backend.Infrastructure.Services
{
    /// <summary>
    /// Local file storage service for chat media files
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _baseStoragePath = configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
            _logger = logger;
            
            // Ensure base directory exists
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }

        #region File Storage Operations

        /// <summary>
        /// Store a file for a chat message with proper organization
        /// </summary>
        public async Task<string> StoreFileAsync(Stream fileStream, string fileName, string contentType, string senderId, string recipientId, string abuneId, MessageType messageType)
        {
            try
            {
                // Generate unique filename to prevent conflicts
                var uniqueFileName = GenerateUniqueFileName(fileName);
                
                // Create directory structure: uploads/chat/{abuneId}/{senderId}/{recipientId}/{messageType}/{date}/
                var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var directoryPath = Path.Combine(_baseStoragePath, "chat", abuneId, senderId, recipientId, messageType.ToString().ToLower(), dateFolder);
                
                // Ensure directory exists
                Directory.CreateDirectory(directoryPath);
                
                // Full file path
                var filePath = Path.Combine(directoryPath, uniqueFileName);
                
                // Store the file
                using (var fileStream2 = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Return the relative URL path for database storage
                var relativeUrl = Path.Combine("uploads", "chat", abuneId, senderId, recipientId, messageType.ToString().ToLower(), dateFolder, uniqueFileName)
                    .Replace("\\", "/"); // Ensure forward slashes for URLs
                
                _logger.LogInformation($"File stored successfully: {relativeUrl}");
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store file");
                throw new Exception("Failed to store file", ex);
            }
        }

        /// <summary>
        /// Store a broadcast file (sent to all community members)
        /// </summary>
        public async Task<string> StoreBroadcastFileAsync(Stream fileStream, string fileName, string contentType, string senderId, string abuneId, MessageType messageType)
        {
            try
            {
                // Generate unique filename
                var uniqueFileName = GenerateUniqueFileName(fileName);
                
                // Create directory structure: uploads/broadcast/{abuneId}/{senderId}/{messageType}/{date}/
                var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var directoryPath = Path.Combine(_baseStoragePath, "broadcast", abuneId, senderId, messageType.ToString().ToLower(), dateFolder);
                
                // Ensure directory exists
                Directory.CreateDirectory(directoryPath);
                
                // Full file path
                var filePath = Path.Combine(directoryPath, uniqueFileName);
                
                // Store the file
                using (var fileStream2 = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Return the relative URL path
                var relativeUrl = Path.Combine("uploads", "broadcast", abuneId, senderId, messageType.ToString().ToLower(), dateFolder, uniqueFileName)
                    .Replace("\\", "/");
                
                _logger.LogInformation($"Broadcast file stored successfully: {relativeUrl}");
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store broadcast file");
                throw new Exception("Failed to store broadcast file", ex);
            }
        }

        /// <summary>
        /// Get file stream for reading
        /// </summary>
        public async Task<Stream> GetFileAsync(string fileUrl)
        {
            try
            {
                var fullPath = Path.Combine(_baseStoragePath, fileUrl.Replace("uploads/", ""));
                
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {fileUrl}");
                }
                
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file: {FileUrl}", fileUrl);
                throw;
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        public async Task DeleteFileAsync(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_baseStoragePath, fileName.Replace("uploads/", ""));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted successfully: {fileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public async Task<FileInfo> GetFileInfoAsync(string fileUrl)
        {
            try
            {
                var fullPath = Path.Combine(_baseStoragePath, fileUrl.Replace("uploads/", ""));
                
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {fileUrl}");
                }
                
                return new FileInfo(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file info: {FileUrl}", fileUrl);
                throw;
            }
        }

        /// <summary>
        /// Upload a file (implements IFileStorageService)
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                // Generate unique filename to prevent conflicts
                var uniqueFileName = GenerateUniqueFileName(fileName);
                
                // Create directory structure: uploads/general/{date}/
                var dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var directoryPath = Path.Combine(_baseStoragePath, "general", dateFolder);
                
                // Ensure directory exists
                Directory.CreateDirectory(directoryPath);
                
                // Full file path
                var filePath = Path.Combine(directoryPath, uniqueFileName);
                
                // Store the file
                using (var fileStream2 = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }
                
                // Return the relative URL path for database storage
                var relativeUrl = Path.Combine("uploads", "general", dateFolder, uniqueFileName)
                    .Replace("\\", "/"); // Ensure forward slashes for URLs
                
                _logger.LogInformation($"File uploaded successfully: {relativeUrl}");
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file");
                throw new Exception("Failed to upload file", ex);
            }
        }

        /// <summary>
        /// Download a file (implements IFileStorageService)
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            return await GetFileAsync(fileName);
        }

        /// <summary>
        /// Get file URL (implements IFileStorageService)
        /// </summary>
        public async Task<string> GetFileUrlAsync(string fileName)
        {
            // For local storage, return the relative URL
            return fileName.StartsWith("uploads/") ? fileName : $"uploads/{fileName}";
        }

        /// <summary>
        /// Check if file exists (implements IFileStorageService)
        /// </summary>
        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(_baseStoragePath, fileName.Replace("uploads/", ""));
                return File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check file existence: {FileName}", fileName);
                return false;
            }
        }

        #endregion

        #region Chat-Specific File Operations

        /// <summary>
        /// Get all files for a specific chat conversation
        /// </summary>
        public async Task<List<ChatFileInfo>> GetChatFilesAsync(string senderId, string recipientId, string abuneId, MessageType? messageType = null)
        {
            try
            {
                var files = new List<ChatFileInfo>();
                var chatPath = Path.Combine(_baseStoragePath, "chat", abuneId, senderId, recipientId);
                
                if (!Directory.Exists(chatPath))
                {
                    return files;
                }
                
                // If specific message type requested, only search that folder
                if (messageType.HasValue)
                {
                    var typePath = Path.Combine(chatPath, messageType.Value.ToString().ToLower());
                    if (Directory.Exists(typePath))
                    {
                        files.AddRange(await GetFilesFromDirectoryAsync(typePath, senderId, recipientId, abuneId, messageType.Value));
                    }
                }
                else
                {
                    // Search all message type folders
                    foreach (var typeDir in Directory.GetDirectories(chatPath))
                    {
                        var type = Path.GetFileName(typeDir);
                        if (Enum.TryParse<MessageType>(type, true, out var messageTypeEnum))
                        {
                            files.AddRange(await GetFilesFromDirectoryAsync(typeDir, senderId, recipientId, abuneId, messageTypeEnum));
                        }
                    }
                }
                
                return files.OrderByDescending(f => f.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chat files");
                return new List<ChatFileInfo>();
            }
        }

        /// <summary>
        /// Get all broadcast files for a community
        /// </summary>
        public async Task<List<ChatFileInfo>> GetBroadcastFilesAsync(string abuneId, MessageType? messageType = null)
        {
            try
            {
                var files = new List<ChatFileInfo>();
                var broadcastPath = Path.Combine(_baseStoragePath, "broadcast", abuneId);
                
                if (!Directory.Exists(broadcastPath))
                {
                    return files;
                }
                
                // Search through all sender folders
                foreach (var senderDir in Directory.GetDirectories(broadcastPath))
                {
                    var senderId = Path.GetFileName(senderDir);
                    
                    if (messageType.HasValue)
                    {
                        var typePath = Path.Combine(senderDir, messageType.Value.ToString().ToLower());
                        if (Directory.Exists(typePath))
                        {
                            files.AddRange(await GetFilesFromDirectoryAsync(typePath, senderId, "", abuneId, messageType.Value, true));
                        }
                    }
                    else
                    {
                        // Search all message type folders
                        foreach (var typeDir in Directory.GetDirectories(senderDir))
                        {
                            var type = Path.GetFileName(typeDir);
                            if (Enum.TryParse<MessageType>(type, true, out var messageTypeEnum))
                            {
                                files.AddRange(await GetFilesFromDirectoryAsync(typeDir, senderId, "", abuneId, messageTypeEnum, true));
                            }
                        }
                    }
                }
                
                return files.OrderByDescending(f => f.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get broadcast files");
                return new List<ChatFileInfo>();
            }
        }

        /// <summary>
        /// Clean up old files (older than specified days)
        /// </summary>
        public async Task<int> CleanupOldFilesAsync(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var deletedCount = 0;
                
                // Clean up chat files
                deletedCount += await CleanupDirectoryAsync(Path.Combine(_baseStoragePath, "chat"), cutoffDate);
                
                // Clean up broadcast files
                deletedCount += await CleanupDirectoryAsync(Path.Combine(_baseStoragePath, "broadcast"), cutoffDate);
                
                _logger.LogInformation($"Cleaned up {deletedCount} old files");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old files");
                return 0;
            }
        }

        #endregion

        #region Private Helper Methods

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            return $"{nameWithoutExtension}_{timestamp}_{randomSuffix}{extension}";
        }

        private async Task<List<ChatFileInfo>> GetFilesFromDirectoryAsync(string directoryPath, string senderId, string recipientId, string abuneId, MessageType messageType, bool isBroadcast = false)
        {
            var files = new List<ChatFileInfo>();
            
            try
            {
                foreach (var dateDir in Directory.GetDirectories(directoryPath))
                {
                    var dateStr = Path.GetFileName(dateDir);
                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        foreach (var filePath in Directory.GetFiles(dateDir))
                        {
                            var fileInfo = new FileInfo(filePath);
                            var relativeUrl = Path.Combine("uploads", isBroadcast ? "broadcast" : "chat", abuneId, senderId, recipientId, messageType.ToString().ToLower(), dateStr, fileInfo.Name)
                                .Replace("\\", "/");
                            
                            files.Add(new ChatFileInfo
                            {
                                FileName = fileInfo.Name,
                                FileUrl = relativeUrl,
                                FileSize = fileInfo.Length,
                                FileType = fileInfo.Extension,
                                MessageType = messageType,
                                SenderId = senderId,
                                RecipientId = recipientId,
                                AbuneId = abuneId,
                                IsBroadcast = isBroadcast,
                                CreatedAt = fileInfo.CreationTimeUtc,
                                LastModified = fileInfo.LastWriteTimeUtc
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get files from directory: {DirectoryPath}", directoryPath);
            }
            
            return files;
        }

        private async Task<int> CleanupDirectoryAsync(string directoryPath, DateTime cutoffDate)
        {
            var deletedCount = 0;
            
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;
                
                foreach (var subDir in Directory.GetDirectories(directoryPath))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (DateTime.TryParse(dirName, out var dirDate) && dirDate < cutoffDate)
                    {
                        try
                        {
                            Directory.Delete(subDir, true);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old directory: {DirectoryPath}", subDir);
                        }
                    }
                    else
                    {
                        // Recursively check subdirectories
                        deletedCount += await CleanupDirectoryAsync(subDir, cutoffDate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup directory: {DirectoryPath}", directoryPath);
            }
            
            return deletedCount;
        }

        #endregion
    }


}
