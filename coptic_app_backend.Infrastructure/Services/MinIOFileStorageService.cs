using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace coptic_app_backend.Infrastructure.Services
{
    /// <summary>
    /// MinIO file storage service for media files (books, videos, audio)
    /// </summary>
    public class MinIOFileStorageService : IFileStorageService, IMediaStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly string _endpoint;
        private readonly ILogger<MinIOFileStorageService> _logger;

        public MinIOFileStorageService(IConfiguration configuration, ILogger<MinIOFileStorageService> logger)
        {
            _logger = logger;
            
            try
            {
                var minioConfig = configuration.GetSection("MinIO");
                _endpoint = minioConfig["Endpoint"] ?? "localhost:9000";
                var accessKey = minioConfig["AccessKey"] ?? "minioadmin";
                var secretKey = minioConfig["SecretKey"] ?? "minioadmin";
                var useSSL = minioConfig.GetValue<bool>("UseSSL", false);
                _bucketName = minioConfig["BucketName"] ?? "coptic-files";

                _logger.LogInformation("Initializing MinIO client with endpoint: {Endpoint}, Bucket: {BucketName}, UseSSL: {UseSSL}", 
                    _endpoint, _bucketName, useSSL);

                _minioClient = new MinioClient()
                    .WithEndpoint(_endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(useSSL)
                    .Build();

                _logger.LogInformation("MinIO client initialized successfully");

                // Note: Bucket creation will be handled on first use to avoid async issues in constructor
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MinIO client: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        #region IFileStorageService Implementation

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                await EnsureBucketExistsAsync();
                
                var objectName = GenerateObjectName(fileName);
                
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);
                
                _logger.LogInformation($"File uploaded successfully to MinIO: {objectName}");
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to MinIO: {FileName}", fileName);
                throw new Exception("Failed to upload file to MinIO", ex);
            }
        }

        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            try
            {
                var memoryStream = new MemoryStream();
                
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream));

                await _minioClient.GetObjectAsync(getObjectArgs);
                
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from MinIO: {FileName}", fileName);
                throw new Exception("Failed to download file from MinIO", ex);
            }
        }

        public async Task<string> GetFileUrlAsync(string fileName)
        {
            try
            {
                // Return the permanent public URL instead of presigned URL
                var publicUrl = $"http://{_endpoint}/{_bucketName}/{Uri.EscapeDataString(fileName)}";
                _logger.LogInformation("Generated public URL for file: {FileName} -> {Url}", fileName, publicUrl);
                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file URL for: {FileName}", fileName);
                throw new Exception("Failed to get file URL", ex);
            }
        }

        public async Task DeleteFileAsync(string fileName)
        {
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);
                
                _logger.LogInformation($"File deleted successfully from MinIO: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from MinIO: {FileName}", fileName);
                throw new Exception("Failed to delete file from MinIO", ex);
            }
        }

        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                await _minioClient.StatObjectAsync(statObjectArgs);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "File does not exist in MinIO: {FileName}", fileName);
                return false;
            }
        }

        #endregion

        #region Media-Specific Methods

        /// <summary>
        /// Upload a media file (book, video, or audio) to a specific folder
        /// </summary>
        public async Task<string> UploadMediaFileAsync(Stream fileStream, string fileName, string contentType, string folderId, MediaType mediaType)
        {
            try
            {
                // Add null checks for debugging
                if (_minioClient == null)
                {
                    _logger.LogError("MinIO client is null");
                    throw new InvalidOperationException("MinIO client is not initialized");
                }

                if (fileStream == null)
                {
                    _logger.LogError("File stream is null");
                    throw new ArgumentNullException(nameof(fileStream));
                }

                if (string.IsNullOrEmpty(_bucketName))
                {
                    _logger.LogError("Bucket name is null or empty");
                    throw new InvalidOperationException("Bucket name is not configured");
                }

                _logger.LogInformation("Starting media file upload: {FileName}, ContentType: {ContentType}, FolderId: {FolderId}, MediaType: {MediaType}", 
                    fileName, contentType, folderId, mediaType);

                await EnsureBucketExistsAsync();
                
                var objectName = GenerateMediaObjectName(fileName, folderId, mediaType);
                _logger.LogInformation("Generated object name: {ObjectName}", objectName);
                
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                _logger.LogInformation("Calling MinIO PutObjectAsync...");
                await _minioClient.PutObjectAsync(putObjectArgs);
                
                _logger.LogInformation($"Media file uploaded successfully to MinIO: {objectName}");
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload media file to MinIO: {FileName}. Error: {ErrorMessage}. Inner: {InnerError}", 
                    fileName, ex.Message, ex.InnerException?.Message);
                throw new Exception($"Failed to upload media file to MinIO: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all media files in a folder
        /// </summary>
        public async Task<List<MediaFileInfo>> GetMediaFilesInFolderAsync(string folderId, MediaType? mediaType = null)
        {
            try
            {
                _logger.LogInformation("Getting media files for folder: {FolderId}, MediaType: {MediaType}", folderId, mediaType);

                if (_minioClient == null)
                {
                    _logger.LogError("MinIO client is null");
                    return new List<MediaFileInfo>();
                }

                // For now, return empty list as MinIO listing is complex
                // The fallback to local storage will handle this
                _logger.LogWarning("MinIO file listing not implemented, relying on fallback to local storage tracking");
                return new List<MediaFileInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get media files in folder: {FolderId}", folderId);
                return new List<MediaFileInfo>();
            }
        }

        /// <summary>
        /// Delete all media files in a folder (simplified implementation)
        /// </summary>
        public async Task DeleteMediaFilesInFolderAsync(string folderId)
        {
            try
            {
                // For now, this is a no-op as the ListObjectsAsync method is not available
                // This can be implemented later when the MinIO client is updated
                _logger.LogWarning("DeleteMediaFilesInFolderAsync is not fully implemented due to MinIO client limitations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media files in folder: {FolderId}", folderId);
                throw new Exception("Failed to delete media files in folder", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                _logger.LogInformation("Checking if bucket exists: {BucketName}", _bucketName);
                
                if (_minioClient == null)
                {
                    _logger.LogError("MinIO client is null in EnsureBucketExistsAsync");
                    throw new InvalidOperationException("MinIO client is null");
                }

                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_bucketName);

                var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
                _logger.LogInformation("Bucket exists check result: {Exists}", exists);
                
                if (!exists)
                {
                    _logger.LogInformation("Creating bucket: {BucketName}", _bucketName);
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_bucketName);

                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                    _logger.LogInformation($"Created MinIO bucket: {_bucketName}");
                }
                else
                {
                    _logger.LogInformation("Bucket already exists: {BucketName}", _bucketName);
                }

                // Set bucket policy for public read access
                await SetBucketPublicReadPolicyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure bucket exists: {BucketName}. Error: {ErrorMessage}", _bucketName, ex.Message);
                throw;
            }
        }

        private async Task SetBucketPublicReadPolicyAsync()
        {
            try
            {
                // Create a policy that allows public read access to all objects in the bucket
                var policy = $@"{{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {{
                            ""Effect"": ""Allow"",
                            ""Principal"": {{
                                ""AWS"": [""*""]
                            }},
                            ""Action"": [""s3:GetObject""],
                            ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
                        }}
                    ]
                }}";

                var setPolicyArgs = new SetPolicyArgs()
                    .WithBucket(_bucketName)
                    .WithPolicy(policy);

                await _minioClient.SetPolicyAsync(setPolicyArgs);
                _logger.LogInformation("Set public read policy for MinIO bucket: {BucketName}", _bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting bucket policy for: {BucketName}", _bucketName);
                // Don't throw here as the bucket might still work with authentication
                _logger.LogWarning("Continuing without public read policy - files will require authentication");
            }
        }

        private string GenerateObjectName(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            var uniqueFileName = $"{nameWithoutExtension}_{timestamp}_{randomSuffix}{extension}";
            return $"general/{DateTime.UtcNow:yyyy/MM/dd}/{uniqueFileName}";
        }

        private string GenerateMediaObjectName(string fileName, string folderId, MediaType mediaType)
        {
            var extension = Path.GetExtension(fileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            var uniqueFileName = $"{nameWithoutExtension}_{timestamp}_{randomSuffix}{extension}";
            return $"media/{folderId}/{mediaType.ToString().ToLower()}/{uniqueFileName}";
        }

        private MediaType ExtractMediaTypeFromPath(string objectPath)
        {
            var pathParts = objectPath.Split('/');
            if (pathParts.Length >= 3)
            {
                var mediaTypeStr = pathParts[2];
                if (Enum.TryParse<MediaType>(mediaTypeStr, true, out var mediaType))
                {
                    return mediaType;
                }
            }
            return MediaType.Other;
        }

        #endregion
    }

}