using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Application.Services
{
    public class FolderService : IFolderService
    {
        private readonly IFolderRepository _folderRepository;
        private readonly ILogger<FolderService> _logger;

        public FolderService(IFolderRepository folderRepository, ILogger<FolderService> logger)
        {
            _folderRepository = folderRepository;
            _logger = logger;
        }

        public async Task<List<Folder>> GetFoldersByAbuneIdAsync(string abuneId)
        {
            try
            {
                return await _folderRepository.GetFoldersByAbuneIdAsync(abuneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<Folder?> GetFolderByIdAsync(string folderId)
        {
            try
            {
                return await _folderRepository.GetFolderByIdAsync(folderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder by ID: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<List<Folder>> GetRootFoldersAsync(string abuneId)
        {
            try
            {
                return await _folderRepository.GetRootFoldersAsync(abuneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root folders for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<List<Folder>> GetChildFoldersAsync(string parentId)
        {
            try
            {
                return await _folderRepository.GetChildFoldersAsync(parentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting child folders for parent: {ParentId}", parentId);
                throw;
            }
        }

        public async Task<List<FolderTreeNode>> GetFolderTreeAsync(string abuneId)
        {
            try
            {
                return await _folderRepository.GetFolderTreeAsync(abuneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder tree for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<List<Folder>> GetFolderPathAsync(string folderId)
        {
            try
            {
                return await _folderRepository.GetFolderPathAsync(folderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder path for: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<Folder> CreateFolderAsync(CreateFolderRequest request, string createdBy, string abuneId)
        {
            try
            {
                // Validate the request
                var validation = await ValidateCreateFolderAsync(request, abuneId);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.ErrorMessage);
                }

                var folder = new Folder
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    ParentId = request.ParentId,
                    CreatedBy = createdBy,
                    AbuneId = abuneId,
                    Color = request.Color,
                    Icon = request.Icon,
                    SortOrder = request.SortOrder
                };

                return await _folderRepository.CreateFolderAsync(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder");
                throw;
            }
        }

        public async Task<Folder> UpdateFolderAsync(string folderId, UpdateFolderRequest request, string updatedBy)
        {
            try
            {
                // Validate the request
                var validation = await ValidateUpdateFolderAsync(folderId, request);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.ErrorMessage);
                }

                var existingFolder = await _folderRepository.GetFolderByIdAsync(folderId);
                if (existingFolder == null)
                {
                    throw new ArgumentException($"Folder with ID {folderId} not found");
                }

                // Update properties
                existingFolder.Name = request.Name.Trim();
                existingFolder.Description = request.Description?.Trim();
                existingFolder.ParentId = request.ParentId;
                existingFolder.Color = request.Color;
                existingFolder.Icon = request.Icon;
                existingFolder.SortOrder = request.SortOrder;
                existingFolder.IsActive = request.IsActive;

                return await _folderRepository.UpdateFolderAsync(folderId, existingFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> DeleteFolderAsync(string folderId, string deletedBy)
        {
            try
            {
                var folder = await _folderRepository.GetFolderByIdAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                return await _folderRepository.DeleteFolderAsync(folderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> PermanentlyDeleteFolderAsync(string folderId, string deletedBy)
        {
            try
            {
                var folder = await _folderRepository.GetFolderByIdAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                return await _folderRepository.PermanentlyDeleteFolderAsync(folderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> MoveFolderAsync(MoveFolderRequest request, string movedBy)
        {
            try
            {
                // Validate the request
                var validation = await ValidateMoveFolderAsync(request);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.ErrorMessage);
                }

                return await _folderRepository.MoveFolderAsync(request.FolderId, request.NewParentId, request.NewSortOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder: {FolderId}", request.FolderId);
                throw;
            }
        }

        public async Task<List<Folder>> SearchFoldersAsync(string abuneId, string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await _folderRepository.GetFoldersByAbuneIdAsync(abuneId);
                }

                return await _folderRepository.SearchFoldersAsync(abuneId, searchTerm.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching folders for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateCreateFolderAsync(CreateFolderRequest request, string abuneId)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return (false, "Folder name is required");
                }

                if (request.Name.Length > 255)
                {
                    return (false, "Folder name cannot exceed 255 characters");
                }

                if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
                {
                    return (false, "Folder description cannot exceed 1000 characters");
                }

                // Validate parent folder exists if specified
                if (!string.IsNullOrEmpty(request.ParentId))
                {
                    var parentFolder = await _folderRepository.GetFolderByIdAsync(request.ParentId);
                    if (parentFolder == null || parentFolder.AbuneId != abuneId)
                    {
                        return (false, "Parent folder not found or does not belong to this Abune");
                    }
                }

                // Check if folder name already exists in the same parent
                var nameExists = await _folderRepository.FolderNameExistsAsync(request.Name, request.ParentId, abuneId);
                if (nameExists)
                {
                    return (false, "A folder with this name already exists in the same location");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating create folder request");
                return (false, "An error occurred while validating the request");
            }
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateUpdateFolderAsync(string folderId, UpdateFolderRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return (false, "Folder name is required");
                }

                if (request.Name.Length > 255)
                {
                    return (false, "Folder name cannot exceed 255 characters");
                }

                if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 1000)
                {
                    return (false, "Folder description cannot exceed 1000 characters");
                }

                // Get existing folder
                var existingFolder = await _folderRepository.GetFolderByIdAsync(folderId);
                if (existingFolder == null)
                {
                    return (false, "Folder not found");
                }

                // Validate parent folder exists if specified
                if (!string.IsNullOrEmpty(request.ParentId))
                {
                    var parentFolder = await _folderRepository.GetFolderByIdAsync(request.ParentId);
                    if (parentFolder == null || parentFolder.AbuneId != existingFolder.AbuneId)
                    {
                        return (false, "Parent folder not found or does not belong to this Abune");
                    }

                    // Check for circular reference
                    if (request.ParentId == folderId)
                    {
                        return (false, "A folder cannot be its own parent");
                    }

                    var canMove = await _folderRepository.CanMoveFolderAsync(folderId, request.ParentId);
                    if (!canMove)
                    {
                        return (false, "Cannot move folder to a descendant folder (would create circular reference)");
                    }
                }

                // Check if folder name already exists in the same parent (excluding current folder)
                var nameExists = await _folderRepository.FolderNameExistsAsync(request.Name, request.ParentId, existingFolder.AbuneId, folderId);
                if (nameExists)
                {
                    return (false, "A folder with this name already exists in the same location");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating update folder request");
                return (false, "An error occurred while validating the request");
            }
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateMoveFolderAsync(MoveFolderRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.FolderId))
                {
                    return (false, "Folder ID is required");
                }

                // Get existing folder
                var existingFolder = await _folderRepository.GetFolderByIdAsync(request.FolderId);
                if (existingFolder == null)
                {
                    return (false, "Folder not found");
                }

                // Validate parent folder exists if specified
                if (!string.IsNullOrEmpty(request.NewParentId))
                {
                    var parentFolder = await _folderRepository.GetFolderByIdAsync(request.NewParentId);
                    if (parentFolder == null || parentFolder.AbuneId != existingFolder.AbuneId)
                    {
                        return (false, "Parent folder not found or does not belong to this Abune");
                    }

                    // Check for circular reference
                    if (request.NewParentId == request.FolderId)
                    {
                        return (false, "A folder cannot be moved to itself");
                    }

                    var canMove = await _folderRepository.CanMoveFolderAsync(request.FolderId, request.NewParentId);
                    if (!canMove)
                    {
                        return (false, "Cannot move folder to a descendant folder (would create circular reference)");
                    }
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating move folder request");
                return (false, "An error occurred while validating the request");
            }
        }

        public async Task<List<Folder>> GetAllFoldersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all folders from repository");
                var folders = await _folderRepository.GetAllFoldersAsync();
                _logger.LogInformation("Retrieved {Count} folders from repository", folders.Count);
                return folders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all folders");
                throw;
            }
        }

        public async Task<List<FolderTreeNode>> GetAllFolderTreesAsync()
        {
            try
            {
                return await _folderRepository.GetAllFolderTreesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all folder trees");
                throw;
            }
        }
    }
}
