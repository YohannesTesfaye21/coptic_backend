using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IMediaFileRepository
    {
        /// <summary>
        /// Get all media files for a specific folder
        /// </summary>
        Task<List<MediaFile>> GetMediaFilesByFolderIdAsync(string folderId, MediaType? mediaType = null);
        
        /// <summary>
        /// Get media file by ID
        /// </summary>
        Task<MediaFile?> GetMediaFileByIdAsync(string fileId);
        
        /// <summary>
        /// Get media file by object name
        /// </summary>
        Task<MediaFile?> GetMediaFileByObjectNameAsync(string objectName);
        
        /// <summary>
        /// Get all media files for a specific Abune
        /// </summary>
        Task<List<MediaFile>> GetMediaFilesByAbuneIdAsync(string abuneId, MediaType? mediaType = null);
        
        /// <summary>
        /// Get all media files uploaded by a specific user
        /// </summary>
        Task<List<MediaFile>> GetMediaFilesByUploaderAsync(string uploaderId, MediaType? mediaType = null);
        
        /// <summary>
        /// Create a new media file record
        /// </summary>
        Task<MediaFile> CreateMediaFileAsync(MediaFile mediaFile);
        
        /// <summary>
        /// Update an existing media file record
        /// </summary>
        Task<MediaFile> UpdateMediaFileAsync(MediaFile mediaFile);
        
        /// <summary>
        /// Soft delete a media file (set IsActive = false)
        /// </summary>
        Task<bool> DeleteMediaFileAsync(string fileId);
        
        /// <summary>
        /// Permanently delete a media file
        /// </summary>
        Task<bool> PermanentlyDeleteMediaFileAsync(string fileId);
        
        /// <summary>
        /// Get media files by storage type
        /// </summary>
        Task<List<MediaFile>> GetMediaFilesByStorageTypeAsync(string storageType, MediaType? mediaType = null);
        
        /// <summary>
        /// Get all media files by media type across all folders
        /// </summary>
        Task<List<MediaFile>> GetAllMediaFilesByTypeAsync(MediaType mediaType);
        
        /// <summary>
        /// Get all media files across all folders and Abunes
        /// </summary>
        Task<List<MediaFile>> GetAllMediaFilesAsync();
        
        /// <summary>
        /// Delete all media files in a folder
        /// </summary>
        Task<bool> DeleteAllMediaFilesInFolderAsync(string folderId);
        
        /// <summary>
        /// Permanently delete all media files in a folder
        /// </summary>
        Task<bool> PermanentlyDeleteAllMediaFilesInFolderAsync(string folderId);
    }
}
