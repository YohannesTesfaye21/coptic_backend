using Microsoft.EntityFrameworkCore;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Infrastructure.Repositories
{
    public class PostgreSQLMediaFileRepository : IMediaFileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostgreSQLMediaFileRepository> _logger;

        public PostgreSQLMediaFileRepository(ApplicationDbContext context, ILogger<PostgreSQLMediaFileRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<MediaFile>> GetMediaFilesByFolderIdAsync(string folderId, MediaType? mediaType = null)
        {
            try
            {
                var query = _context.MediaFiles
                    .Where(f => f.FolderId == folderId && f.IsActive);

                if (mediaType.HasValue)
                {
                    query = query.Where(f => f.MediaType == mediaType.Value);
                }

                return await query
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media files for folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<MediaFile?> GetMediaFileByIdAsync(string fileId)
        {
            try
            {
                return await _context.MediaFiles
                    .Include(f => f.Folder)
                    .Include(f => f.UploadedByUser)
                    .Include(f => f.Abune)
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media file by ID: {FileId}", fileId);
                throw;
            }
        }

        public async Task<MediaFile?> GetMediaFileByObjectNameAsync(string objectName)
        {
            try
            {
                return await _context.MediaFiles
                    .FirstOrDefaultAsync(f => f.ObjectName == objectName && f.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media file by object name: {ObjectName}", objectName);
                throw;
            }
        }

        public async Task<List<MediaFile>> GetMediaFilesByAbuneIdAsync(string abuneId, MediaType? mediaType = null)
        {
            try
            {
                var query = _context.MediaFiles
                    .Where(f => f.AbuneId == abuneId && f.IsActive);

                if (mediaType.HasValue)
                {
                    query = query.Where(f => f.MediaType == mediaType.Value);
                }

                return await query
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media files for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<List<MediaFile>> GetMediaFilesByUploaderAsync(string uploaderId, MediaType? mediaType = null)
        {
            try
            {
                var query = _context.MediaFiles
                    .Where(f => f.UploadedBy == uploaderId && f.IsActive);

                if (mediaType.HasValue)
                {
                    query = query.Where(f => f.MediaType == mediaType.Value);
                }

                return await query
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media files for uploader: {UploaderId}", uploaderId);
                throw;
            }
        }

        public async Task<MediaFile> CreateMediaFileAsync(MediaFile mediaFile)
        {
            try
            {
                _context.MediaFiles.Add(mediaFile);
                await _context.SaveChangesAsync();
                return mediaFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating media file: {FileName}", mediaFile.FileName);
                throw;
            }
        }

        public async Task<MediaFile> UpdateMediaFileAsync(MediaFile mediaFile)
        {
            try
            {
                mediaFile.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _context.MediaFiles.Update(mediaFile);
                await _context.SaveChangesAsync();
                return mediaFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating media file: {FileId}", mediaFile.Id);
                throw;
            }
        }

        public async Task<bool> DeleteMediaFileAsync(string fileId)
        {
            try
            {
                var mediaFile = await _context.MediaFiles.FindAsync(fileId);
                if (mediaFile == null)
                    return false;

                mediaFile.IsActive = false;
                mediaFile.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting media file: {FileId}", fileId);
                throw;
            }
        }

        public async Task<bool> PermanentlyDeleteMediaFileAsync(string fileId)
        {
            try
            {
                var mediaFile = await _context.MediaFiles.FindAsync(fileId);
                if (mediaFile == null)
                    return false;

                _context.MediaFiles.Remove(mediaFile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting media file: {FileId}", fileId);
                throw;
            }
        }

        public async Task<List<MediaFile>> GetMediaFilesByStorageTypeAsync(string storageType, MediaType? mediaType = null)
        {
            try
            {
                var query = _context.MediaFiles
                    .Where(f => f.StorageType == storageType && f.IsActive);

                if (mediaType.HasValue)
                {
                    query = query.Where(f => f.MediaType == mediaType.Value);
                }

                return await query
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media files by storage type: {StorageType}", storageType);
                throw;
            }
        }
    }
}
