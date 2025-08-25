using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// File upload controller for chat media files
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IFileStorageService fileStorageService, ILogger<FileUploadController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        #region Chat File Upload

        /// <summary>
        /// Upload a file for a chat message
        /// </summary>
        /// <param name="request">File upload request with chat context</param>
        /// <returns>File upload result with URL</returns>
        [HttpPost("chat")]
        public async Task<ActionResult<FileUploadResponse>> UploadChatFile([FromForm] ChatFileUploadRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Validate request
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                if (string.IsNullOrEmpty(request.RecipientId))
                {
                    return BadRequest("Recipient ID is required");
                }

                if (request.MessageType == MessageType.Text)
                {
                    return BadRequest("Text messages cannot have files");
                }

                // Validate file size (e.g., max 50MB)
                if (request.File.Length > 50 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds maximum limit of 50MB");
                }

                // Validate file type based on message type
                if (!IsValidFileType(request.File, request.MessageType))
                {
                    return BadRequest($"Invalid file type for {request.MessageType} message");
                }

                // Store the file
                using var stream = request.File.OpenReadStream();
                var fileUrl = await _fileStorageService.StoreFileAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    currentUserId,
                    request.RecipientId,
                    currentUserAbuneId,
                    request.MessageType
                );

                // Get file info
                var fileInfo = await _fileStorageService.GetFileInfoAsync(fileUrl);

                var response = new FileUploadResponse
                {
                    FileUrl = fileUrl,
                    FileName = request.File.FileName,
                    FileSize = request.File.Length,
                    FileType = request.File.ContentType,
                    MessageType = request.MessageType,
                    VoiceDuration = request.VoiceDuration
                };

                _logger.LogInformation($"File uploaded successfully: {fileUrl} by user {currentUserId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload chat file");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Upload a file for a broadcast message (Abune only)
        /// </summary>
        /// <param name="request">Broadcast file upload request</param>
        /// <returns>File upload result with URL</returns>
        [HttpPost("broadcast")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<FileUploadResponse>> UploadBroadcastFile([FromForm] BroadcastFileUploadRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                // Validate request
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest("No file provided");
                }

                if (request.MessageType == MessageType.Text)
                {
                    return BadRequest("Text messages cannot have files");
                }

                // Validate file size (e.g., max 50MB)
                if (request.File.Length > 50 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds maximum limit of 50MB");
                }

                // Validate file type based on message type
                if (!IsValidFileType(request.File, request.MessageType))
                {
                    return BadRequest($"Invalid file type for {request.MessageType} message");
                }

                // Store the file
                using var stream = request.File.OpenReadStream();
                var fileUrl = await _fileStorageService.StoreBroadcastFileAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    currentUserId,
                    currentUserAbuneId,
                    request.MessageType
                );

                var response = new FileUploadResponse
                {
                    FileUrl = fileUrl,
                    FileName = request.File.FileName,
                    FileSize = request.File.Length,
                    FileType = request.File.ContentType,
                    MessageType = request.MessageType,
                    VoiceDuration = request.VoiceDuration
                };

                _logger.LogInformation($"Broadcast file uploaded successfully: {fileUrl} by Abune {currentUserId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload broadcast file");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region File Management

        /// <summary>
        /// Get chat files for a conversation
        /// </summary>
        /// <param name="recipientId">Other participant's user ID</param>
        /// <param name="messageType">Optional message type filter</param>
        /// <returns>List of chat files</returns>
        [HttpGet("chat/{recipientId}")]
        public async Task<ActionResult<List<ChatFileInfo>>> GetChatFiles(string recipientId, [FromQuery] MessageType? messageType = null)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var files = await _fileStorageService.GetChatFilesAsync(currentUserId, recipientId, currentUserAbuneId, messageType);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chat files");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get broadcast files for the community
        /// </summary>
        /// <param name="messageType">Optional message type filter</param>
        /// <returns>List of broadcast files</returns>
        [HttpGet("broadcast")]
        public async Task<ActionResult<List<ChatFileInfo>>> GetBroadcastFiles([FromQuery] MessageType? messageType = null)
        {
            try
            {
                var currentUserAbuneId = User.FindFirst("AbuneId")?.Value;

                if (string.IsNullOrEmpty(currentUserAbuneId))
                {
                    return BadRequest("User information not found in token");
                }

                var files = await _fileStorageService.GetBroadcastFilesAsync(currentUserAbuneId, messageType);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get broadcast files");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a file (only file owner can delete)
        /// </summary>
        /// <param name="fileUrl">File URL to delete</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{fileUrl}")]
        public async Task<ActionResult> DeleteFile(string fileUrl)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                // TODO: Add validation that user owns the file or is Abune
                var success = await _fileStorageService.DeleteFileAsync(fileUrl);
                
                if (success)
                {
                    _logger.LogInformation($"File deleted successfully: {fileUrl} by user {currentUserId}");
                    return Ok(new { message = "File deleted successfully" });
                }

                return BadRequest("Failed to delete file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Private Helper Methods

        private bool IsValidFileType(IFormFile file, MessageType messageType)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            return messageType switch
            {
                MessageType.Image => IsValidImageFile(extension, contentType),
                MessageType.Document => IsValidDocumentFile(extension, contentType),
                MessageType.Voice => IsValidVoiceFile(extension, contentType),
                _ => false
            };
        }

        private bool IsValidImageFile(string extension, string contentType)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            var validContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };

            return validExtensions.Contains(extension) && validContentTypes.Contains(contentType);
        }

        private bool IsValidDocumentFile(string extension, string contentType)
        {
            var validExtensions = new[] { ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
            var validContentTypes = new[] { 
                "application/pdf", "text/plain", "application/msword", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

            return validExtensions.Contains(extension) && validContentTypes.Contains(contentType);
        }

        private bool IsValidVoiceFile(string extension, string contentType)
        {
            var validExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" };
            var validContentTypes = new[] { 
                "audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4", "audio/aac", "audio/flac" 
            };

            return validExtensions.Contains(extension) && validContentTypes.Contains(contentType);
        }

        #endregion
    }

    #region Request and Response Models

    public class ChatFileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string RecipientId { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
        public int? VoiceDuration { get; set; } // Duration in seconds for voice messages
    }

    public class BroadcastFileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public MessageType MessageType { get; set; } = MessageType.Text;
        public int? VoiceDuration { get; set; } // Duration in seconds for voice messages
    }

    public class FileUploadResponse
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public int? VoiceDuration { get; set; }
    }

    #endregion
}
