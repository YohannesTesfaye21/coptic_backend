using Microsoft.EntityFrameworkCore;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace coptic_app_backend.Infrastructure.Repositories
{
    public class PostgreSQLFolderRepository : IFolderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostgreSQLFolderRepository> _logger;

        public PostgreSQLFolderRepository(ApplicationDbContext context, ILogger<PostgreSQLFolderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Folder>> GetFoldersByAbuneIdAsync(string abuneId)
        {
            try
            {
                return await _context.Folders
                    .Where(f => f.AbuneId == abuneId && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders by Abune ID: {AbuneId}", abuneId);
                throw;
            }
        }

        public async Task<Folder?> GetFolderByIdAsync(string folderId)
        {
            try
            {
                return await _context.Folders
                    .Include(f => f.Parent)
                    .Include(f => f.Children.Where(c => c.IsActive))
                    .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive);
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
                return await _context.Folders
                    .Where(f => f.AbuneId == abuneId && f.ParentId == null && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();
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
                return await _context.Folders
                    .Where(f => f.ParentId == parentId && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();
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
                var folders = await _context.Folders
                    .Where(f => f.AbuneId == abuneId && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();

                return BuildFolderTree(folders, null);
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
                var path = new List<Folder>();
                var currentFolder = await GetFolderByIdAsync(folderId);
                
                if (currentFolder == null)
                    return path;

                // Build path from current folder to root
                var folder = currentFolder;
                while (folder != null)
                {
                    path.Insert(0, folder);
                    if (folder.ParentId != null)
                    {
                        folder = await _context.Folders
                            .FirstOrDefaultAsync(f => f.Id == folder.ParentId && f.IsActive);
                    }
                    else
                    {
                        folder = null;
                    }
                }

                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder path for: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<Folder> CreateFolderAsync(Folder folder)
        {
            try
            {
                if (string.IsNullOrEmpty(folder.Id))
                {
                    folder.Id = Guid.NewGuid().ToString();
                }

                folder.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                folder.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder created successfully: {FolderId}", folder.Id);
                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder");
                throw;
            }
        }

        public async Task<Folder> UpdateFolderAsync(string folderId, Folder folder)
        {
            try
            {
                var existingFolder = await _context.Folders.FindAsync(folderId);
                if (existingFolder == null)
                {
                    throw new ArgumentException($"Folder with ID {folderId} not found");
                }

                // Update properties
                existingFolder.Name = folder.Name;
                existingFolder.Description = folder.Description;
                existingFolder.ParentId = folder.ParentId;
                existingFolder.Color = folder.Color;
                existingFolder.Icon = folder.Icon;
                existingFolder.SortOrder = folder.SortOrder;
                existingFolder.IsActive = folder.IsActive;
                existingFolder.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder updated successfully: {FolderId}", folderId);
                return existingFolder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> DeleteFolderAsync(string folderId)
        {
            try
            {
                var folder = await _context.Folders.FindAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                // Soft delete - set IsActive to false
                folder.IsActive = false;
                folder.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Also soft delete all children
                await SoftDeleteChildrenAsync(folderId);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder soft deleted successfully: {FolderId}", folderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> PermanentlyDeleteFolderAsync(string folderId)
        {
            try
            {
                var folder = await _context.Folders.FindAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                // Permanently delete all children first
                await PermanentlyDeleteChildrenAsync(folderId);

                // Then delete the folder itself
                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder permanently deleted successfully: {FolderId}", folderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> MoveFolderAsync(string folderId, string? newParentId, int? newSortOrder)
        {
            try
            {
                var folder = await _context.Folders.FindAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                folder.ParentId = newParentId;
                if (newSortOrder.HasValue)
                {
                    folder.SortOrder = newSortOrder.Value;
                }
                folder.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder moved successfully: {FolderId}", folderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> FolderNameExistsAsync(string name, string? parentId, string abuneId, string? excludeFolderId = null)
        {
            try
            {
                var query = _context.Folders
                    .Where(f => f.Name == name && f.ParentId == parentId && f.AbuneId == abuneId && f.IsActive);

                if (!string.IsNullOrEmpty(excludeFolderId))
                {
                    query = query.Where(f => f.Id != excludeFolderId);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if folder name exists");
                throw;
            }
        }

        public async Task<List<Folder>> GetFolderDescendantsAsync(string folderId)
        {
            try
            {
                var descendants = new List<Folder>();
                await GetDescendantsRecursiveAsync(folderId, descendants);
                return descendants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder descendants: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<bool> CanMoveFolderAsync(string folderId, string? newParentId)
        {
            try
            {
                // Cannot move folder to itself
                if (folderId == newParentId)
                    return false;

                // If moving to null (root), it's always allowed
                if (string.IsNullOrEmpty(newParentId))
                    return true;

                // Check if newParentId is a descendant of folderId (would create circular reference)
                var descendants = await GetFolderDescendantsAsync(folderId);
                return !descendants.Any(d => d.Id == newParentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if folder can be moved");
                throw;
            }
        }

        public async Task<bool> UpdateFolderSortOrderAsync(string folderId, int newSortOrder)
        {
            try
            {
                var folder = await _context.Folders.FindAsync(folderId);
                if (folder == null)
                {
                    return false;
                }

                folder.SortOrder = newSortOrder;
                folder.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Folder sort order updated successfully: {FolderId}", folderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder sort order: {FolderId}", folderId);
                throw;
            }
        }

        public async Task<List<Folder>> SearchFoldersAsync(string abuneId, string searchTerm)
        {
            try
            {
                var term = searchTerm.ToLower();
                return await _context.Folders
                    .Where(f => f.AbuneId == abuneId && f.IsActive && 
                               (f.Name.ToLower().Contains(term) || 
                                (f.Description != null && f.Description.ToLower().Contains(term))))
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching folders for Abune: {AbuneId}", abuneId);
                throw;
            }
        }

        #region Private Helper Methods

        private List<FolderTreeNode> BuildFolderTree(List<Folder> folders, string? parentId)
        {
            var tree = new List<FolderTreeNode>();
            var children = folders.Where(f => f.ParentId == parentId).ToList();

            foreach (var folder in children)
            {
                var node = new FolderTreeNode
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Description = folder.Description,
                    ParentId = folder.ParentId,
                    CreatedBy = folder.CreatedBy,
                    AbuneId = folder.AbuneId,
                    CreatedAt = folder.CreatedAt,
                    LastModified = folder.LastModified,
                    IsActive = folder.IsActive,
                    SortOrder = folder.SortOrder,
                    Color = folder.Color,
                    Icon = folder.Icon,
                    Children = BuildFolderTree(folders, folder.Id),
                    ChildrenCount = folders.Count(f => f.ParentId == folder.Id && f.IsActive)
                };

                // Calculate total children count (including all descendants)
                node.TotalChildrenCount = CalculateTotalChildrenCount(node.Children);

                tree.Add(node);
            }

            return tree;
        }

        private int CalculateTotalChildrenCount(List<FolderTreeNode> children)
        {
            int count = children.Count;
            foreach (var child in children)
            {
                count += CalculateTotalChildrenCount(child.Children);
            }
            return count;
        }

        private async Task SoftDeleteChildrenAsync(string parentId)
        {
            var children = await _context.Folders
                .Where(f => f.ParentId == parentId && f.IsActive)
                .ToListAsync();

            foreach (var child in children)
            {
                child.IsActive = false;
                child.LastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await SoftDeleteChildrenAsync(child.Id);
            }
        }

        private async Task PermanentlyDeleteChildrenAsync(string parentId)
        {
            var children = await _context.Folders
                .Where(f => f.ParentId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                await PermanentlyDeleteChildrenAsync(child.Id);
                _context.Folders.Remove(child);
            }
        }

        private async Task GetDescendantsRecursiveAsync(string parentId, List<Folder> descendants)
        {
            var children = await _context.Folders
                .Where(f => f.ParentId == parentId && f.IsActive)
                .ToListAsync();

            foreach (var child in children)
            {
                descendants.Add(child);
                await GetDescendantsRecursiveAsync(child.Id, descendants);
            }
        }

        public async Task<List<Folder>> GetAllFoldersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all folders from database");
                var allFolders = await _context.Folders
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} total folders from database (including inactive)", allFolders.Count);
                
                var activeFolders = allFolders.Where(f => f.IsActive).ToList();
                _logger.LogInformation("Retrieved {Count} active folders from database", activeFolders.Count);
                
                return activeFolders;
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
                var allFolders = await _context.Folders
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .ToListAsync();

                // Group folders by AbuneId and build trees for each Abune
                var trees = new List<FolderTreeNode>();
                var abuneGroups = allFolders.GroupBy(f => f.AbuneId);

                foreach (var abuneGroup in abuneGroups)
                {
                    var abuneFolders = abuneGroup.ToList();
                    var abuneTrees = BuildFolderTreesForAbune(abuneFolders);
                    trees.AddRange(abuneTrees);
                }

                return trees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all folder trees");
                throw;
            }
        }

        private List<FolderTreeNode> BuildFolderTreesForAbune(List<Folder> folders)
        {
            var tree = new List<FolderTreeNode>();
            var rootFolders = folders.Where(f => f.ParentId == null).ToList();

            foreach (var folder in rootFolders)
            {
                var node = new FolderTreeNode
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Description = folder.Description,
                    ParentId = folder.ParentId,
                    CreatedBy = folder.CreatedBy,
                    AbuneId = folder.AbuneId,
                    CreatedAt = folder.CreatedAt,
                    LastModified = folder.LastModified,
                    IsActive = folder.IsActive,
                    SortOrder = folder.SortOrder,
                    Color = folder.Color,
                    Icon = folder.Icon,
                    Children = BuildFolderTree(folders, folder.Id),
                    ChildrenCount = folders.Count(f => f.ParentId == folder.Id && f.IsActive)
                };

                // Calculate total children count (including all descendants)
                node.TotalChildrenCount = CalculateTotalChildrenCount(node.Children);

                tree.Add(node);
            }

            return tree;
        }

        #endregion
    }
}
