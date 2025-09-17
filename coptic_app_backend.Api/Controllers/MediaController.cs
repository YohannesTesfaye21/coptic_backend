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

        /// <summary>
        /// Sanitize filename to avoid issues with spaces and special characters
        /// </summary>
        /// <param name="filename">Original filename</param>
        /// <returns>Sanitized filename</returns>
        private string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return filename;

            // Replace spaces with underscores and remove other problematic characters
            var sanitized = filename
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("{", "")
                .Replace("}", "")
                .Replace("&", "and")
                .Replace("+", "plus")
                .Replace("=", "equals")
                .Replace("?", "")
                .Replace("#", "")
                .Replace("%", "")
                .Replace("@", "at")
                .Replace("!", "")
                .Replace("$", "")
                .Replace("^", "")
                .Replace("*", "")
                .Replace("|", "")
                .Replace("\\", "")
                .Replace("/", "_")
                .Replace(":", "")
                .Replace(";", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace(",", "")
                .Replace(".", "_");

            // Remove multiple consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            return sanitized;
        }

        /// <summary>
        /// Get content type based on file extension
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Content type</returns>
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".webm" => "video/webm",
                ".mkv" => "video/x-matroska",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".flac" => "audio/flac",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Handle range requests for video/audio streaming with mobile optimization
        /// </summary>
        /// <param name="fileStream">File stream</param>
        /// <param name="contentType">Content type</param>
        /// <param name="fileName">File name</param>
        /// <param name="rangeHeader">Range header value</param>
        /// <returns>Partial content response</returns>
        private IActionResult HandleRangeRequest(Stream fileStream, string contentType, string fileName, string rangeHeader)
        {
            try
            {
                var fileLength = fileStream.Length;
                var range = ParseRangeHeader(rangeHeader, fileLength);
                
                if (range == null)
                {
                    return BadRequest("Invalid range header");
                }

                // Mobile optimization: limit chunk size for better performance
                const long maxChunkSize = 2 * 1024 * 1024; // 2MB chunks for mobile
                var chunkSize = range.Value.End - range.Value.Start + 1;
                
                if (chunkSize > maxChunkSize)
                {
                    range = (range.Value.Start, range.Value.Start + maxChunkSize - 1);
                    chunkSize = maxChunkSize;
                }

                Response.StatusCode = 206; // Partial Content
                Response.Headers["Content-Range"] = $"bytes {range.Value.Start}-{range.Value.End}/{fileLength}";
                Response.Headers["Content-Length"] = chunkSize.ToString();
                
                // Mobile streaming headers
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                Response.Headers["Content-Disposition"] = "inline";
                
                // Add streaming-specific headers for mobile
                if (contentType.StartsWith("video/"))
                {
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
                    Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                }

                // Create a partial stream with buffering for mobile
                var partialStream = new BufferedPartialStream(fileStream, range.Value.Start, chunkSize);
                
                return File(partialStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling range request: {RangeHeader}", rangeHeader);
                return StatusCode(500, new { error = "Error handling range request", message = ex.Message });
            }
        }

        /// <summary>
        /// Parse range header
        /// </summary>
        /// <param name="rangeHeader">Range header value</param>
        /// <param name="fileLength">File length</param>
        /// <returns>Range object or null if invalid</returns>
        private (long Start, long End)? ParseRangeHeader(string rangeHeader, long fileLength)
        {
            if (string.IsNullOrEmpty(rangeHeader) || !rangeHeader.StartsWith("bytes="))
                return null;

            var range = rangeHeader.Substring(6); // Remove "bytes="
            var parts = range.Split('-');
            
            if (parts.Length != 2)
                return null;

            long start = 0;
            long end = fileLength - 1;

            if (!string.IsNullOrEmpty(parts[0]))
            {
                if (!long.TryParse(parts[0], out start))
                    return null;
            }

            if (!string.IsNullOrEmpty(parts[1]))
            {
                if (!long.TryParse(parts[1], out end))
                    return null;
            }

            if (start < 0 || end >= fileLength || start > end)
                return null;

            return (start, end);
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

                // Sanitize filename for object storage (but keep original for display)
                var originalFileName = request.File.FileName;
                var sanitizedFileName = SanitizeFilename(originalFileName);
                _logger.LogInformation("Original filename: {Original}, Sanitized: {Sanitized}", originalFileName, sanitizedFileName);

                // Upload the file - try MinIO first, fallback to local storage
                using var stream = request.File.OpenReadStream();
                string objectName;
                string fileUrl;

                try
                {
                    _logger.LogInformation("Attempting to upload to MinIO...");
                    objectName = await _mediaService.UploadMediaFileAsync(
                        stream,
                        sanitizedFileName,
                        request.File.ContentType,
                        request.FolderId,
                        request.MediaType
                    );

                    // Generate download URL instead of presigned URL
                    fileUrl = $"{Request.Scheme}://{Request.Host}/api/Media/download/{Uri.EscapeDataString(objectName)}";
                    _logger.LogInformation("Successfully uploaded to MinIO: {ObjectName}", objectName);
                    
                    // Save file record to database
                    var mediaFile = new MediaFile
                    {
                        FileName = originalFileName, // Store original filename for display
                        ObjectName = objectName,     // Store sanitized object name for storage
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
                        sanitizedFileName,
                        request.File.ContentType
                    );
                    
                    // For local storage fallback, generate MinIO-style URL
                    fileUrl = $"http://162.243.165.212:9000/coptic-files/{Uri.EscapeDataString(objectName)}";
                    _logger.LogInformation("Successfully uploaded to local storage: {ObjectName}", objectName);
                    
                    // Save file record to database
                    var mediaFile = new MediaFile
                    {
                        FileName = originalFileName, // Store original filename for display
                        ObjectName = objectName,     // Store sanitized object name for storage
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
        /// Test MinIO connectivity
        /// </summary>
        /// <returns>MinIO connection status</returns>
        [HttpGet("test-minio")]
        [AllowAnonymous]
        public async Task<IActionResult> TestMinIO()
        {
            try
            {
                _logger.LogInformation("Testing MinIO connectivity");
                
                // Try to get a test file URL to verify connectivity
                var testUrl = await _mediaService.GetFileUrlAsync("test-connection");
                
                return Ok(new { 
                    status = "MinIO is accessible", 
                    message = "MinIO connection successful",
                    testUrl = testUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO connectivity test failed");
                return StatusCode(500, new { 
                    status = "MinIO connection failed", 
                    error = ex.Message,
                    message = "MinIO is not accessible"
                });
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
        /// Stream a media file with mobile optimization
        /// </summary>
        /// <param name="objectName">MinIO object name</param>
        /// <returns>File stream</returns>
        [HttpGet("stream/{objectName}")]
        [AllowAnonymous]
        public async Task<IActionResult> StreamMediaFile(string objectName)
        {
            try
            {
                // URL decode the object name
                var decodedObjectName = Uri.UnescapeDataString(objectName);
                _logger.LogInformation("Stream request for object: {ObjectName} (decoded: {DecodedObjectName})", objectName, decodedObjectName);

                // First try to find the file in the database to determine storage type
                var mediaFiles = await _mediaFileRepository.GetAllMediaFilesAsync();
                var mediaFile = mediaFiles.FirstOrDefault(f => f.ObjectName == decodedObjectName);
                
                Stream fileStream;
                string fileName = Path.GetFileName(decodedObjectName);
                
                if (mediaFile != null)
                {
                    _logger.LogInformation("Found file in database: {ObjectName}, StorageType: {StorageType}", decodedObjectName, mediaFile.StorageType);
                    
                    if (mediaFile.StorageType == "MinIO")
                    {
                        // Download from MinIO
                        try
                        {
                            fileStream = await _mediaService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception minioEx)
                        {
                            _logger.LogError(minioEx, "MinIO download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from MinIO", message = minioEx.Message });
                        }
                    }
                    else
                    {
                        // Download from local storage
                        try
                        {
                            fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception localEx)
                        {
                            _logger.LogError(localEx, "Local storage download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from local storage", message = localEx.Message });
                        }
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
                        try
                        {
                            fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception localEx)
                        {
                            _logger.LogError(localEx, "Both MinIO and local storage download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from any storage", message = "File not found in MinIO or local storage" });
                        }
                    }
                }

                // Determine content type based on file extension
                string contentType = GetContentType(fileName);
                
                // Mobile-optimized headers for streaming
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Cache-Control"] = "public, max-age=86400"; // 24 hours cache
                Response.Headers["ETag"] = $"\"{decodedObjectName.GetHashCode()}\"";
                Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
                
                // For mobile streaming, don't force download - let browser handle it
                if (contentType.StartsWith("video/") || contentType.StartsWith("audio/"))
                {
                    Response.Headers["Content-Disposition"] = "inline";
                }
                else
                {
                    Response.Headers["Content-Disposition"] = $"attachment; filename=\"{Uri.EscapeDataString(fileName)}\"";
                }
                
                // Check if this is a range request (for video streaming)
                var rangeHeader = Request.Headers["Range"].FirstOrDefault();
                if (!string.IsNullOrEmpty(rangeHeader) && (contentType.StartsWith("video/") || contentType.StartsWith("audio/")))
                {
                    return HandleRangeRequest(fileStream, contentType, fileName, rangeHeader);
                }
                
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stream media file: {ObjectName}", objectName);
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
                var mediaFiles = await _mediaFileRepository.GetAllMediaFilesAsync();
                var mediaFile = mediaFiles.FirstOrDefault(f => f.ObjectName == decodedObjectName);
                
                Stream fileStream;
                string fileName = Path.GetFileName(decodedObjectName);
                
                if (mediaFile != null)
                {
                    _logger.LogInformation("Found file in database: {ObjectName}, StorageType: {StorageType}", decodedObjectName, mediaFile.StorageType);
                    
                    if (mediaFile.StorageType == "MinIO")
                    {
                        // Download from MinIO
                        try
                        {
                            fileStream = await _mediaService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception minioEx)
                        {
                            _logger.LogError(minioEx, "MinIO download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from MinIO", message = minioEx.Message });
                        }
                    }
                    else
                    {
                        // Download from local storage
                        try
                        {
                            fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception localEx)
                        {
                            _logger.LogError(localEx, "Local storage download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from local storage", message = localEx.Message });
                        }
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
                        try
                        {
                            fileStream = await _fileStorageService.DownloadFileAsync(decodedObjectName);
                        }
                        catch (Exception localEx)
                        {
                            _logger.LogError(localEx, "Both MinIO and local storage download failed for: {ObjectName}", decodedObjectName);
                            return StatusCode(500, new { error = "Failed to download file from any storage", message = "File not found in MinIO or local storage" });
                        }
                    }
                }

                // Determine content type based on file extension
                string contentType = GetContentType(fileName);
                
                // Enable range requests for video streaming
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Cache-Control"] = "public, max-age=3600";
                
                // Add headers for proper mobile download support
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{Uri.EscapeDataString(fileName)}\"";
                Response.Headers["Content-Length"] = fileStream.Length.ToString();
                
                // Check if this is a range request (for video streaming)
                var rangeHeader = Request.Headers["Range"].FirstOrDefault();
                if (!string.IsNullOrEmpty(rangeHeader) && (contentType.StartsWith("video/") || contentType.StartsWith("audio/")))
                {
                    return HandleRangeRequest(fileStream, contentType, fileName, rangeHeader);
                }
                
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download media file: {ObjectName}", objectName);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get a download URL for a media file
        /// </summary>
        /// <param name="objectName">MinIO object name or filename</param>
        /// <returns>Download URL</returns>
        [HttpGet("url/{objectName}")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> GetMediaFileUrl(string objectName)
        {
            try
            {
                // Decode the object name
                var decodedObjectName = Uri.UnescapeDataString(objectName);
                _logger.LogInformation("Getting URL for object: {ObjectName}", decodedObjectName);

                // First, try to find the file in the database by filename or object name
                var mediaFile = await _mediaFileRepository.GetMediaFileByObjectNameAsync(decodedObjectName);
                
                string actualObjectName = decodedObjectName;
                
                if (mediaFile == null)
                {
                    // If not found by object name, try to find by filename
                    var files = await _mediaFileRepository.GetAllMediaFilesAsync();
                    mediaFile = files.FirstOrDefault(f => f.FileName == decodedObjectName);
                    
                    if (mediaFile != null)
                    {
                        actualObjectName = mediaFile.ObjectName;
                        _logger.LogInformation("Found file by filename, using object name: {ObjectName}", actualObjectName);
                    }
                    else
                    {
                        _logger.LogWarning("File not found in database: {ObjectName}", decodedObjectName);
                        return NotFound("File not found");
                    }
                }

                // Return download URL instead of presigned URL
                // This uses the download endpoint which works reliably
                var downloadUrl = $"{Request.Scheme}://{Request.Host}/api/Media/download/{Uri.EscapeDataString(actualObjectName)}";
                _logger.LogInformation("Generated download URL for: {ObjectName} -> {Url}", actualObjectName, downloadUrl);
                
                return Ok(new { url = downloadUrl });
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
                _logger.LogInformation("Database lookup for object '{ObjectName}' returned: {Found}", decodedObjectName, mediaFile != null ? "Found" : "Not Found");
                
                if (mediaFile != null)
                {
                    _logger.LogInformation("Found file in database: {ObjectName}, StorageType: {StorageType}", decodedObjectName, mediaFile.StorageType);
                    
                    try
                    {
                        bool deleted = false;
                        if (mediaFile.StorageType == "MinIO")
                        {
                            // Try MinIO first
                            try
                            {
                                await _mediaService.DeleteFileAsync(decodedObjectName);
                                _logger.LogInformation("Successfully deleted from MinIO: {ObjectName}", decodedObjectName);
                                deleted = true;
                            }
                            catch (Exception minioEx)
                            {
                                _logger.LogWarning(minioEx, "MinIO deletion failed for {ObjectName}, trying local storage", decodedObjectName);
                                // Fallback to local storage if MinIO fails
                                try
                                {
                                    await _fileStorageService.DeleteFileAsync(decodedObjectName);
                                    _logger.LogInformation("Successfully deleted from local storage (fallback): {ObjectName}", decodedObjectName);
                                    deleted = true;
                                }
                                catch (Exception localEx)
                                {
                                    _logger.LogError(localEx, "Both MinIO and local storage deletion failed for {ObjectName}", decodedObjectName);
                                    throw new Exception($"Failed to delete file from both MinIO and local storage. MinIO error: {minioEx.Message}, Local error: {localEx.Message}", localEx);
                                }
                            }
                        }
                        else
                        {
                            // Delete from local storage
                            await _fileStorageService.DeleteFileAsync(decodedObjectName);
                            _logger.LogInformation("Successfully deleted from local storage: {ObjectName}", decodedObjectName);
                            deleted = true;
                        }
                        
                        if (!deleted)
                        {
                            throw new Exception("File deletion failed from both storage systems");
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
                    _logger.LogWarning("File not found in database, trying both storage systems: {ObjectName}", decodedObjectName);
                    
                    // Fallback: try both storage systems
                    bool deleted = false;
                    Exception? minioEx = null;
                    Exception? localEx = null;
                    
                    // Try MinIO first
                    try
                    {
                        await _mediaService.DeleteFileAsync(decodedObjectName);
                        _logger.LogInformation("Media file deleted successfully from MinIO: {ObjectName} by user {UserId}", decodedObjectName, currentUserId);
                        deleted = true;
                    }
                    catch (Exception ex)
                    {
                        minioEx = ex;
                        _logger.LogWarning(ex, "MinIO delete failed: {ObjectName}", decodedObjectName);
                    }
                    
                    // Try local storage
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(decodedObjectName);
                        _logger.LogInformation("Media file deleted successfully from local storage: {ObjectName} by user {UserId}", decodedObjectName, currentUserId);
                        deleted = true;
                    }
                    catch (Exception ex)
                    {
                        localEx = ex;
                        _logger.LogWarning(ex, "Local storage delete failed: {ObjectName}", decodedObjectName);
                    }
                    
                    if (!deleted)
                    {
                        var errorMessage = "File deletion failed from both storage systems";
                        if (minioEx != null && localEx != null)
                        {
                            errorMessage += $". MinIO error: {minioEx.Message}, Local error: {localEx.Message}";
                        }
                        else if (minioEx != null)
                        {
                            errorMessage += $". MinIO error: {minioEx.Message}";
                        }
                        else if (localEx != null)
                        {
                            errorMessage += $". Local error: {localEx.Message}";
                        }
                        
                        throw new Exception(errorMessage);
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

    /// <summary>
    /// Stream that provides a partial view of another stream
    /// </summary>
    public class PartialStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _start;
        private readonly long _length;
        private long _position;

        public PartialStream(Stream baseStream, long start, long length)
        {
            _baseStream = baseStream;
            _start = start;
            _length = length;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            var remaining = _length - _position;
            var bytesToRead = (int)Math.Min(count, remaining);

            _baseStream.Position = _start + _position;
            var bytesRead = _baseStream.Read(buffer, offset, bytesToRead);
            _position += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentException("Invalid seek origin")
            };

            if (newPosition < 0 || newPosition > _length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _position = newPosition;
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// Buffered partial stream optimized for mobile streaming
    /// </summary>
    public class BufferedPartialStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _start;
        private readonly long _length;
        private long _position;
        private readonly byte[] _buffer;
        private int _bufferPosition;
        private int _bufferLength;
        private const int BufferSize = 64 * 1024; // 64KB buffer for mobile

        public BufferedPartialStream(Stream baseStream, long start, long length)
        {
            _baseStream = baseStream;
            _start = start;
            _length = length;
            _position = 0;
            _buffer = new byte[BufferSize];
            _bufferPosition = 0;
            _bufferLength = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            var remaining = _length - _position;
            var bytesToRead = (int)Math.Min(count, remaining);
            var totalBytesRead = 0;

            while (bytesToRead > 0)
            {
                // Refill buffer if needed
                if (_bufferPosition >= _bufferLength)
                {
                    var bufferStart = _start + _position;
                    var bufferSize = (int)Math.Min(BufferSize, _length - _position);
                    
                    if (bufferSize <= 0)
                        break;

                    _baseStream.Position = bufferStart;
                    _bufferLength = _baseStream.Read(_buffer, 0, bufferSize);
                    _bufferPosition = 0;

                    if (_bufferLength == 0)
                        break;
                }

                // Copy from buffer
                var bytesFromBuffer = Math.Min(bytesToRead, _bufferLength - _bufferPosition);
                Array.Copy(_buffer, _bufferPosition, buffer, offset + totalBytesRead, bytesFromBuffer);
                
                _bufferPosition += bytesFromBuffer;
                _position += bytesFromBuffer;
                totalBytesRead += bytesFromBuffer;
                bytesToRead -= bytesFromBuffer;
            }

            return totalBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentException("Invalid seek origin")
            };

            if (newPosition < 0 || newPosition > _length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _position = newPosition;
            _bufferPosition = 0; // Reset buffer position
            _bufferLength = 0;   // Invalidate buffer
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    #endregion
}
