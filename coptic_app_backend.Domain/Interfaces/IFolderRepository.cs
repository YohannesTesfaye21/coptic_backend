using System.Collections.Generic;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    public interface IFolderRepository
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
        Task<Folder> CreateFolderAsync(Folder folder);
        
        /// <summary>
        /// Update an existing folder
        /// </summary>
        Task<Folder> UpdateFolderAsync(string folderId, Folder folder);
        
        /// <summary>
        /// Delete a folder (soft delete by setting IsActive = false)
        /// </summary>
        Task<bool> DeleteFolderAsync(string folderId);
        
        /// <summary>
        /// Permanently delete a folder and all its children
        /// </summary>
        Task<bool> PermanentlyDeleteFolderAsync(string folderId);
        
        /// <summary>
        /// Move a folder to a new parent
        /// </summary>
        Task<bool> MoveFolderAsync(string folderId, string? newParentId, int? newSortOrder);
        
        /// <summary>
        /// Check if a folder name exists within the same parent
        /// </summary>
        Task<bool> FolderNameExistsAsync(string name, string? parentId, string abuneId, string? excludeFolderId = null);
        
        /// <summary>
        /// Get all descendants of a folder (children, grandchildren, etc.)
        /// </summary>
        Task<List<Folder>> GetFolderDescendantsAsync(string folderId);
        
        /// <summary>
        /// Check if a folder can be moved to a new parent (prevents circular references)
        /// </summary>
        Task<bool> CanMoveFolderAsync(string folderId, string? newParentId);
        
        /// <summary>
        /// Update folder sort order
        /// </summary>
        Task<bool> UpdateFolderSortOrderAsync(string folderId, int newSortOrder);
        
        /// <summary>
        /// Get folders by search term
        /// </summary>
        Task<List<Folder>> SearchFoldersAsync(string abuneId, string searchTerm);
        
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
