using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// Folder model for hierarchical folder structure with parent-child relationships
    /// </summary>
    public class Folder
    {
        /// <summary>
        /// Unique folder identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Folder name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Folder description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Parent folder ID (null for root folders)
        /// </summary>
        public string? ParentId { get; set; }
        
        /// <summary>
        /// User ID who created this folder
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Abune ID for community-based folder organization
        /// </summary>
        public string AbuneId { get; set; } = string.Empty;
        
        /// <summary>
        /// Folder creation timestamp (Unix timestamp)
        /// </summary>
        public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        /// <summary>
        /// Last modification timestamp (Unix timestamp)
        /// </summary>
        public long LastModified { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        /// <summary>
        /// Whether the folder is active/visible
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Folder sort order within the same parent
        /// </summary>
        public int SortOrder { get; set; } = 0;
        
        /// <summary>
        /// Folder color for UI display (hex color code)
        /// </summary>
        public string? Color { get; set; }
        
        /// <summary>
        /// Folder icon for UI display
        /// </summary>
        public string? Icon { get; set; }
        
        /// <summary>
        /// Navigation properties for Entity Framework
        /// </summary>
        [JsonIgnore]
        public Folder? Parent { get; set; }
        [JsonIgnore]
        public List<Folder> Children { get; set; } = new List<Folder>();
        [JsonIgnore]
        public User? CreatedByUser { get; set; }
        [JsonIgnore]
        public User? Abune { get; set; }
    }
    
    /// <summary>
    /// Folder tree node for hierarchical display
    /// </summary>
    public class FolderTreeNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string AbuneId { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public long LastModified { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public List<FolderTreeNode> Children { get; set; } = new List<FolderTreeNode>();
        public int ChildrenCount { get; set; }
        public int TotalChildrenCount { get; set; } // Including all descendants
    }
    
    /// <summary>
    /// Request model for creating a new folder
    /// </summary>
    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; } = 0;
    }
    
    /// <summary>
    /// Request model for updating a folder
    /// </summary>
    public class UpdateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
    
    /// <summary>
    /// Request model for moving a folder to a new parent
    /// </summary>
    public class MoveFolderRequest
    {
        public string FolderId { get; set; } = string.Empty;
        public string? NewParentId { get; set; }
        public int? NewSortOrder { get; set; }
    }
}
