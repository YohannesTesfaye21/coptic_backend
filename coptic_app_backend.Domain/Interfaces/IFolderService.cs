using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IFolderService
    {
        /// <summary>
        /// Get all folders for a specific Abune
        /// </summary>
        Task<List<Folder>> GetFoldersByAbuneIdAsync(string abuneId);
        
        /// <summary>
        /// Get folder by ID
        /// </summary>
        Task<Folder?> GetFolderByIdAsync(string folderId);
        
        /// <summary>
        /// Get root folders (folders with no parent) for a specific Abune
        /// </summary>
        Task<List<Folder>> GetRootFoldersAsync(string abuneId);
        
        /// <summary>
        /// Get child folders of a specific parent folder
        /// </summary>
        Task<List<Folder>> GetChildFoldersAsync(string parentId);
        
        /// <summary>
        /// Get folder tree structure for a specific Abune
        /// </summary>
        Task<List<FolderTreeNode>> GetFolderTreeAsync(string abuneId);
        
        /// <summary>
        /// Get folder path (breadcrumb) from root to specific folder
        /// </summary>
        Task<List<Folder>> GetFolderPathAsync(string folderId);
        
        /// <summary>
        /// Create a new folder
        /// </summary>
        Task<Folder> CreateFolderAsync(CreateFolderRequest request, string createdBy, string abuneId);
        
        /// <summary>
        /// Update an existing folder
        /// </summary>
        Task<Folder> UpdateFolderAsync(string folderId, UpdateFolderRequest request, string updatedBy);
        
        /// <summary>
        /// Delete a folder (soft delete)
        /// </summary>
        Task<bool> DeleteFolderAsync(string folderId, string deletedBy);
        
        /// <summary>
        /// Permanently delete a folder and all its children
        /// </summary>
        Task<bool> PermanentlyDeleteFolderAsync(string folderId, string deletedBy);
        
        /// <summary>
        /// Move a folder to a new parent
        /// </summary>
        Task<bool> MoveFolderAsync(MoveFolderRequest request, string movedBy);
        
        /// <summary>
        /// Search folders by name or description
        /// </summary>
        Task<List<Folder>> SearchFoldersAsync(string abuneId, string searchTerm);
        
        /// <summary>
        /// Validate folder creation request
        /// </summary>
        Task<(bool IsValid, string ErrorMessage)> ValidateCreateFolderAsync(CreateFolderRequest request, string abuneId);
        
        /// <summary>
        /// Validate folder update request
        /// </summary>
        Task<(bool IsValid, string ErrorMessage)> ValidateUpdateFolderAsync(string folderId, UpdateFolderRequest request);
        
        /// <summary>
        /// Validate folder move request
        /// </summary>
        Task<(bool IsValid, string ErrorMessage)> ValidateMoveFolderAsync(MoveFolderRequest request);
        
        /// <summary>
        /// Get all folders across all Abunes (for public access)
        /// </summary>
        Task<List<Folder>> GetAllFoldersAsync();
        
        /// <summary>
        /// Get all folder trees across all Abunes (for public access)
        /// </summary>
        Task<List<FolderTreeNode>> GetAllFolderTreesAsync();
    }
}
