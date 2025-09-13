using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    /// <summary>
    /// Interface for media storage operations
    /// </summary>
    public interface IMediaStorageService
    {
        Task<string> UploadMediaFileAsync(Stream fileStream, string fileName, string contentType, string folderId, MediaType mediaType);
        Task<List<MediaFileInfo>> GetMediaFilesInFolderAsync(string folderId, MediaType? mediaType = null);
        Task DeleteMediaFilesInFolderAsync(string folderId);
        Task<Stream> DownloadFileAsync(string fileName);
        Task<string> GetFileUrlAsync(string fileName);
        Task DeleteFileAsync(string fileName);
    }
}
