using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using System.Linq;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// Media upload controller for books, videos, and audio files
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaStorageService _mediaService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFolderService _folderService;
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            IMediaStorageService mediaService,
            IFileStorageService fileStorageService,
            IFolderService folderService,
            IMediaFileRepository mediaFileRepository,
            ILogger<MediaController> logger)
        {
            _mediaService = mediaService;
            _fileStorageService = fileStorageService;
            _folderService = folderService;
            _mediaFileRepository = mediaFileRepository;
            _logger = logger;
        }

        #region Media Upload Endpoints

        /// <summary>
        /// Upload a media file (book, video, or audio) to a specific folder
        /// </summary>
        /// <param name="request">Media upload request containing file, folder ID, and media type</param>
        /// <returns>Upload result with file information</returns>
        [HttpPost("upload")]
        [Authorize(Policy = "AbuneOnly")]
        [RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5GB limit
        public async Task<ActionResult<MediaUploadResponse>> UploadMedia([FromForm] MediaUploadRequest request)
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

                if (string.IsNullOrEmpty(request.FolderId))
                {
                    return BadRequest("Folder ID is required");
                }

                // Log the received media type for debugging
                _logger.LogInformation("Received MediaType: {MediaType} (Value: {Value})", request.MediaType, (int)request.MediaType);

                // Validate media type - handle both string and integer values
                if (!Enum.IsDefined(typeof(MediaType), request.MediaType))
                {
                    return BadRequest("Invalid media type. Supported types: Book (0), Video (1), Audio (2). You can use either the name or number.");
                }

                // Verify folder exists and user has access
                var folder = await _folderService.GetFolderByIdAsync(request.FolderId);
                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                if (folder.AbuneId != currentUserAbuneId)
                {
                    return Forbid("You don't have access to this folder");
                }

                // Validate file type based on media type
                var validationResult = ValidateFileForMediaType(request.File, request.MediaType);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.ErrorMessage);
                }

                // Upload the file - try MinIO first, fallback to local storage
                using var stream = request.File.OpenReadStream();
                string objectName;
                string fileUrl;

                try
                {
                    _logger.LogInformation("Attempting to upload to MinIO...");
                    objectName = await _mediaService.UploadMediaFileAsync(
                        stream,
                        request.File.FileName,
                        request.File.ContentType,
                        request.FolderId,
                        request.MediaType
                    );

                    // Get presigned URL
                    fileUrl = await _mediaService.GetFileUrlAsync(objectName);
                    _logger.LogInformation("Successfully uploaded to MinIO: {ObjectName}", objectName);
                    
                    // Save file record to database
                    var mediaFile = new MediaFile
                    {
                        FileName = request.File.FileName,
                        ObjectName = objectName,
                        FileUrl = fileUrl,
                        FileSize = request.File.Length,
                        ContentType = request.File.ContentType,
                        MediaType = request.MediaType,
                        FolderId = request.FolderId,
                        UploadedBy = currentUserId,
                        AbuneId = currentUserAbuneId,
                        Description = request.Description,
                        StorageType = "MinIO"
                    };
                    
                    await _mediaFileRepository.CreateMediaFileAsync(mediaFile);
                    _logger.LogInformation("Saved media file to database: {FileName} in folder {FolderId}", request.File.FileName, request.FolderId);
                }
                catch (Exception minioEx)
                {
                    _logger.LogWarning(minioEx, "MinIO upload failed, falling back to local storage: {ErrorMessage}", minioEx.Message);
                    
                    // Fallback to local storage
                    stream.Position = 0; // Reset stream position
                    objectName = await _fileStorageService.UploadFileAsync(
                        stream,
                        request.File.FileName,
                        request.File.ContentType
                    );
                    
                    // For local storage fallback, generate MinIO-style URL
                    fileUrl = $"http://162.243.165.212:9000/coptic-files/{Uri.EscapeDataString(objectName)}";
                    _logger.LogInformation("Successfully uploaded to local storage: {ObjectName}", objectName);
                    
                    // Save file record to database
                    var mediaFile = new MediaFile
                    {
                        FileName = request.File.FileName,
                        ObjectName = objectName,
                        FileUrl = fileUrl,
                        FileSize = request.File.Length,
                        ContentType = request.File.ContentType,
                        MediaType = request.MediaType,
                        FolderId = request.FolderId,
                        UploadedBy = currentUserId,
                        AbuneId = currentUserAbuneId,
                        Description = request.Description,
                        StorageType = "Local"
                    };
                    
                    await _mediaFileRepository.CreateMediaFileAsync(mediaFile);
                    _logger.LogInformation("Saved media file to database: {FileName} in folder {FolderId}", request.File.FileName, request.FolderId);
                }

                var response = new MediaUploadResponse
                {
                    ObjectName = objectName,
                    FileName = request.File.FileName,
                    FileSize = request.File.Length,
                    FileType = request.File.ContentType,
                    MediaType = request.MediaType,
                    FolderId = request.FolderId,
                    FileUrl = fileUrl,
                    UploadedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"{request.MediaType} uploaded successfully: {objectName} by user {currentUserId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload {MediaType}: {ErrorMessage}", request.MediaType, ex.Message);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        #endregion

        #region Media Management Endpoints

        /// <summary>
        /// Get all media files in a folder
        /// </summary>
        /// <param name="folderId">Folder ID</param>
        /// <param name="mediaType">Optional media type filter</param>
        /// <returns>List of media files in the folder</returns>
        [HttpGet("folder/{folderId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<MediaFileInfo>>> GetMediaFilesInFolder(string folderId, [FromQuery] MediaType? mediaType = null)
        {
            try
            {
                // Verify folder exists
                var folder = await _folderService.GetFolderByIdAsync(folderId);
                if (folder == null)
                {
                    return NotFound("Folder not found");
                }

                // Use folder name for organizing files
                var folderName = folder.Name;

                // Get files from database
                var mediaFiles = await _mediaFileRepository.GetMediaFilesByFolderIdAsync(folderId, mediaType);
                _logger.LogInformation("Retrieved {Count} files from database for folder {FolderId}", mediaFiles.Count, folderId);

                // Convert to MediaFileInfo format
                var files = mediaFiles.Select(f => new MediaFileInfo
                {
                    FileName = f.FileName,
                    ObjectName = f.ObjectName,
                    FileUrl = ConvertToFullUrl(f.FileUrl, f.ObjectName),
                    FileSize = f.FileSize,
                    LastModified = DateTimeOffset.FromUnixTimeSeconds(f.LastModified).DateTime,
                    MediaType = f.MediaType,
                    FolderId = f.FolderId
                }).ToList();

                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get media files in folder: {FolderId}", folderId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get all media files by media type across all folders
        /// </summary>
        /// <param name="mediaType">Media type filter (0=Book, 1=Video, 2=Audio, 3=Other)</param>
        /// <returns>List of media files of the specified type</returns>
        [HttpGet("type/{mediaType}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<MediaFileInfo>>> GetMediaFilesByType(MediaType mediaType)
        {
            try
            {
                _logger.LogInformation("Getting media files by type: {MediaType}", mediaType);

                // Get all media files of the specified type from database
                var mediaFiles = await _mediaFileRepository.GetAllMediaFilesByTypeAsync(mediaType);
                _logger.LogInformation("Retrieved {Count} files of type {MediaType} from database", mediaFiles.Count, mediaType);

                // Convert to MediaFileInfo format
                var files = mediaFiles.Select(f => new MediaFileInfo
                {
                    FileName = f.FileName,
                    ObjectName = f.ObjectName,
                    FileUrl = ConvertToFullUrl(f.FileUrl, f.ObjectName),
                    FileSize = f.FileSize,
                    LastModified = DateTimeOffset.FromUnixTimeSeconds(f.LastModified).DateTime,
                    MediaType = f.MediaType,
                    FolderId = f.FolderId
                }).ToList();

                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media files by type: {MediaType}", mediaType);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Download a media file
        /// </summary>
        /// <param name="objectName">MinIO object name</param>
        /// <returns>File stream</returns>
        [HttpGet("download/{objectName}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadMediaFile(string objectName)
        {
            try
            {
                // URL decode the object name
                var decodedObjectName = Uri.UnescapeDataString(objectName);
                _logger.LogInformation("Download request for object: {ObjectName} (decoded: {DecodedObjectName})", objectName, decodedObjectName);

                // First try to find the file in the database to determine storage type
                var mediaFiles = await _mediaFileRepository.GetMediaFilesByFolderIdAsync("", null);
                var mediaFile = mediaFiles.FirstOrDefault(f => f.ObjectName == decodedObjectName);
                
                Stream fileStream;
                string fileName = Path.GetFileName(decodedObjectName);
                
                if (mediaFile != null)
                {
                    _logger.LogInformation("Found file in database: {ObjectName}, StorageType: {StorageType}", decodedObjectName, mediaFile.StorageType);
                    
                    if (mediaFile.StorageType == "MinIO")
                    {
                        // Download from MinIO
                        fileStream = await _mediaService.DownloadFileAsync(decodedObjectName);
                    }
                    else
                    {
                        // Download from local storage
                        fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                    }
                }
                else
                {
                    _logger.LogWarning("File not found in database, trying MinIO first: {ObjectName}", decodedObjectName);
                    
                    // Fallback: try MinIO first, then local storage
                    try
                    {
                        fileStream = await _mediaService.DownloadFileAsync(decodedObjectName);
                    }
                    catch (Exception minioEx)
                    {
                        _logger.LogWarning(minioEx, "MinIO download failed, trying local storage: {ObjectName}", decodedObjectName);
                        fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                    }
                }

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download media file: {ObjectName}", objectName);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get a presigned URL for a media file
        /// </summary>
        /// <param name="objectName">MinIO object name</param>
        /// <returns>Presigned URL</returns>
        [HttpGet("url/{objectName}")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> GetMediaFileUrl(string objectName)
        {
            try
            {
                var fileUrl = await _mediaService.GetFileUrlAsync(objectName);
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get media file URL: {ObjectName}", objectName);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a media file
        /// </summary>
        /// <param name="objectName">MinIO object name</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{objectName}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> DeleteMediaFile(string objectName)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return BadRequest("User information not found in token");
                }

                // URL decode the object name
                var decodedObjectName = Uri.UnescapeDataString(objectName);
                _logger.LogInformation("Delete request for object: {ObjectName} (decoded: {DecodedObjectName})", objectName, decodedObjectName);

                // First try to find the file in the database to determine storage type
                var mediaFile = await _mediaFileRepository.GetMediaFileByObjectNameAsync(decodedObjectName);
                
                if (mediaFile != null)
                {
                    _logger.LogInformation("Found file in database: {ObjectName}, StorageType: {StorageType}", decodedObjectName, mediaFile.StorageType);
                    
                    try
                    {
                        if (mediaFile.StorageType == "MinIO")
                        {
                            // Delete from MinIO
                            await _mediaService.DeleteFileAsync(decodedObjectName);
                            _logger.LogInformation("Successfully deleted from MinIO: {ObjectName}", decodedObjectName);
                        }
                        else
                        {
                            // Delete from local storage
                            await _fileStorageService.DeleteFileAsync(decodedObjectName);
                            _logger.LogInformation("Successfully deleted from local storage: {ObjectName}", decodedObjectName);
                        }
                        
                        // Delete from database
                        var dbDeleteResult = await _mediaFileRepository.DeleteMediaFileAsync(mediaFile.Id);
                        if (dbDeleteResult)
                        {
                            _logger.LogInformation("Successfully deleted database record for: {ObjectName}", decodedObjectName);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to delete database record for: {ObjectName}", decodedObjectName);
                        }
                        
                        _logger.LogInformation("Media file deleted successfully from {StorageType}: {ObjectName} by user {UserId}", mediaFile.StorageType, decodedObjectName, currentUserId);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete file from {StorageType}: {ObjectName}", mediaFile.StorageType, decodedObjectName);
                        throw new Exception($"Failed to delete file from {mediaFile.StorageType}: {deleteEx.Message}", deleteEx);
                    }
                }
                else
                {
                    _logger.LogWarning("File not found in database, trying MinIO first: {ObjectName}", decodedObjectName);
                    
                    // Fallback: try MinIO first, then local storage
                    bool deleted = false;
                    try
                    {
                        await _mediaService.DeleteFileAsync(decodedObjectName);
                        _logger.LogInformation("Media file deleted successfully from MinIO: {ObjectName} by user {UserId}", decodedObjectName, currentUserId);
                        deleted = true;
                    }
                    catch (Exception minioEx)
                    {
                        _logger.LogWarning(minioEx, "MinIO delete failed, trying local storage: {ObjectName}", decodedObjectName);
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(decodedObjectName);
                            _logger.LogInformation("Media file deleted successfully from local storage: {ObjectName} by user {UserId}", decodedObjectName, currentUserId);
                            deleted = true;
                        }
                        catch (Exception localEx)
                        {
                            _logger.LogError(localEx, "Both MinIO and local storage delete failed for: {ObjectName}", decodedObjectName);
                            throw new Exception($"Failed to delete file from both MinIO and local storage. MinIO error: {minioEx.Message}, Local error: {localEx.Message}", localEx);
                        }
                    }
                    
                    if (!deleted)
                    {
                        throw new Exception("File deletion failed from both storage systems");
                    }
                }

                return Ok(new { message = "Media file deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media file: {ObjectName}", objectName);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        #endregion

        #region Private Helper Methods

        private FileValidationResult ValidateFileForMediaType(IFormFile file, MediaType mediaType)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            return mediaType switch
            {
                MediaType.Book => ValidateBookFile(file, extension, contentType),
                MediaType.Video => ValidateVideoFile(file, extension, contentType),
                MediaType.Audio => ValidateAudioFile(file, extension, contentType),
                _ => new FileValidationResult { IsValid = false, ErrorMessage = "Unsupported media type" }
            };
        }

        private FileValidationResult ValidateBookFile(IFormFile file, string extension, string contentType)
        {
            var validExtensions = new[] { ".pdf", ".epub", ".mobi", ".txt", ".doc", ".docx" };
            var validContentTypes = new[] { 
                "application/pdf", 
                "application/epub+zip", 
                "application/x-mobipocket-ebook",
                "text/plain", 
                "application/msword", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
            };

            if (!validExtensions.Contains(extension) || !validContentTypes.Contains(contentType))
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid file type for book. Supported formats: PDF, EPUB, MOBI, TXT, DOC, DOCX" 
                };
            }

            // Validate file size (max 5GB for books)
            if (file.Length > 5L * 1024 * 1024 * 1024)
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "File size exceeds maximum limit of 5GB for books" 
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        private FileValidationResult ValidateVideoFile(IFormFile file, string extension, string contentType)
        {
            var validExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" };
            var validContentTypes = new[] { 
                "video/mp4", 
                "video/x-msvideo", 
                "video/quicktime", 
                "video/x-ms-wmv", 
                "video/x-flv", 
                "video/webm" 
            };

            if (!validExtensions.Contains(extension) || !validContentTypes.Contains(contentType))
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid file type for video. Supported formats: MP4, AVI, MOV, WMV, FLV, WEBM" 
                };
            }

            // Validate file size (max 5GB for videos)
            if (file.Length > 5L * 1024 * 1024 * 1024)
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "File size exceeds maximum limit of 5GB for videos" 
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        private FileValidationResult ValidateAudioFile(IFormFile file, string extension, string contentType)
        {
            var validExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" };
            var validContentTypes = new[] { 
                "audio/mpeg", 
                "audio/wav", 
                "audio/ogg", 
                "audio/mp4", 
                "audio/aac", 
                "audio/flac" 
            };

            if (!validExtensions.Contains(extension) || !validContentTypes.Contains(contentType))
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid file type for audio. Supported formats: MP3, WAV, OGG, M4A, AAC, FLAC" 
                };
            }

            // Validate file size (max 5GB for audio)
            if (file.Length > 5L * 1024 * 1024 * 1024)
            {
                return new FileValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "File size exceeds maximum limit of 5GB for audio" 
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Convert relative URLs to full MinIO URLs
        /// </summary>
        private string ConvertToFullUrl(string fileUrl, string objectName)
        {
            // If it's already a full URL, return as is
            if (fileUrl.StartsWith("http://") || fileUrl.StartsWith("https://"))
            {
                return fileUrl;
            }
            
            // If it's a relative URL, convert to full MinIO URL
            if (fileUrl.StartsWith("/api/media/download/"))
            {
                // Extract the object name from the relative URL
                var relativeObjectName = fileUrl.Replace("/api/media/download/", "");
                return $"http://162.243.165.212:9000/coptic-files/{Uri.EscapeDataString(relativeObjectName)}";
            }
            
            // If it's just an object name, use it directly
            return $"http://162.243.165.212:9000/coptic-files/{Uri.EscapeDataString(objectName)}";
        }

        #endregion
    }

    #region Request and Response Models

    public class MediaUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string FolderId { get; set; } = string.Empty;
        public MediaType MediaType { get; set; }
        public string? Description { get; set; }
    }

    public class MediaUploadResponse
    {
        public string ObjectName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public MediaType MediaType { get; set; }
        public string FolderId { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    #endregion
}
