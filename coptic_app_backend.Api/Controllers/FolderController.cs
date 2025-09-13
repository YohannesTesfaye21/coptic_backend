using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using coptic_app_backend.Domain.Interfaces;
using coptic_app_backend.Domain.Models;
using coptic_app_backend.Api.Attributes;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace coptic_app_backend.Api.Controllers
{
    /// <summary>
    /// Folder management controller for hierarchical folder structure
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly ILogger<FolderController> _logger;

        public FolderController(IFolderService folderService, ILogger<FolderController> logger)
        {
            _folderService = folderService;
            _logger = logger;
        }

        #region Get Operations

        /// <summary>
        /// Get all folders for the current Abune's community in hierarchical structure
        /// </summary>
        /// <returns>Hierarchical list of folders</returns>
        /// <response code="200">Folders retrieved successfully</response>
        /// <response code="400">Abune ID not found in token</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<FolderTreeNode>>> GetFolders()
        {
            try
            {
                // Get all folders - for public access, we'll get folders for all Abunes
                // This is a simplified approach - in production you might want to add a specific method
                var folders = await _folderService.GetFolderTreeAsync(""); // Empty string to get all
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get folder by ID
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <returns>Folder details</returns>
        /// <response code="200">Folder retrieved successfully</response>
        /// <response code="404">Folder not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Folder>> GetFolder(string id)
        {
            try
            {
                var folder = await _folderService.GetFolderByIdAsync(id);
                if (folder == null)
                {
                    return NotFound($"Folder with ID {id} not found");
                }

                return Ok(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder: {FolderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get root folders (folders with no parent)
        /// </summary>
        /// <returns>List of root folders</returns>
        /// <response code="200">Root folders retrieved successfully</response>
        /// <response code="400">Abune ID not found in token</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("root")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Folder>>> GetRootFolders()
        {
            try
            {
                // Get root folders for all Abunes
                var folders = await _folderService.GetRootFoldersAsync("");
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root folders");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get child folders of a specific parent folder
        /// </summary>
        /// <param name="parentId">Parent folder ID</param>
        /// <returns>List of child folders</returns>
        /// <response code="200">Child folders retrieved successfully</response>
        /// <response code="404">Parent folder not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("parent/{parentId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Folder>>> GetChildFolders(string parentId)
        {
            try
            {
                var folders = await _folderService.GetChildFoldersAsync(parentId);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting child folders for parent: {ParentId}", parentId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get folder tree structure
        /// </summary>
        /// <returns>Hierarchical folder tree</returns>
        /// <response code="200">Folder tree retrieved successfully</response>
        /// <response code="400">Abune ID not found in token</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<ActionResult<List<FolderTreeNode>>> GetFolderTree()
        {
            try
            {
                // Get folder tree for all Abunes
                var tree = await _folderService.GetFolderTreeAsync("");
                return Ok(tree);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder tree");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get folder path (breadcrumb) from root to specific folder
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <returns>Folder path from root to folder</returns>
        /// <response code="200">Folder path retrieved successfully</response>
        /// <response code="404">Folder not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}/path")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Folder>>> GetFolderPath(string id)
        {
            try
            {
                var path = await _folderService.GetFolderPathAsync(id);
                if (path == null || !path.Any())
                {
                    return NotFound($"Folder with ID {id} not found");
                }

                return Ok(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder path for: {FolderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search folders by name or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>List of matching folders</returns>
        /// <response code="200">Search results retrieved successfully</response>
        /// <response code="400">Abune ID not found in token or invalid search term</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<List<Folder>>> SearchFolders([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                // Search folders across all Abunes
                var folders = await _folderService.SearchFoldersAsync("", searchTerm);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching folders");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Create Operations

        /// <summary>
        /// Create a new folder
        /// </summary>
        /// <param name="request">Folder creation request</param>
        /// <returns>Created folder</returns>
        /// <response code="201">Folder created successfully</response>
        /// <response code="400">Invalid request data or validation failed</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<Folder>> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                var abuneId = GetAbuneIdFromToken();
                if (string.IsNullOrEmpty(abuneId))
                {
                    return BadRequest("Abune ID not found in token");
                }

                var createdBy = GetUserIdFromToken();
                var folder = await _folderService.CreateFolderAsync(request, createdBy, abuneId);
                
                return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Update an existing folder
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <param name="request">Folder update request</param>
        /// <returns>Updated folder</returns>
        /// <response code="200">Folder updated successfully</response>
        /// <response code="400">Invalid request data or validation failed</response>
        /// <response code="404">Folder not found</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult<Folder>> UpdateFolder(string id, [FromBody] UpdateFolderRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                var updatedBy = GetUserIdFromToken();
                var folder = await _folderService.UpdateFolderAsync(id, request, updatedBy);
                
                return Ok(folder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Folder with ID {id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder: {FolderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Move a folder to a new parent
        /// </summary>
        /// <param name="request">Folder move request</param>
        /// <returns>Success status</returns>
        /// <response code="200">Folder moved successfully</response>
        /// <response code="400">Invalid request data or validation failed</response>
        /// <response code="404">Folder not found</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("move")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> MoveFolder([FromBody] MoveFolderRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                var movedBy = GetUserIdFromToken();
                var success = await _folderService.MoveFolderAsync(request, movedBy);
                
                if (!success)
                {
                    return NotFound($"Folder with ID {request.FolderId} not found");
                }

                return Ok(new { message = "Folder moved successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving folder: {FolderId}", request.FolderId);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Delete Operations

        /// <summary>
        /// Delete a folder (soft delete)
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <returns>Success status</returns>
        /// <response code="200">Folder deleted successfully</response>
        /// <response code="404">Folder not found</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> DeleteFolder(string id)
        {
            try
            {
                var deletedBy = GetUserIdFromToken();
                var success = await _folderService.DeleteFolderAsync(id, deletedBy);
                
                if (!success)
                {
                    return NotFound($"Folder with ID {id} not found");
                }

                return Ok(new { message = "Folder deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder: {FolderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Permanently delete a folder and all its children
        /// </summary>
        /// <param name="id">Folder ID</param>
        /// <returns>Success status</returns>
        /// <response code="200">Folder permanently deleted successfully</response>
        /// <response code="404">Folder not found</response>
        /// <response code="403">Access denied - Abune only</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}/permanent")]
        [Authorize(Policy = "AbuneOnly")]
        public async Task<ActionResult> PermanentlyDeleteFolder(string id)
        {
            try
            {
                var deletedBy = GetUserIdFromToken();
                var success = await _folderService.PermanentlyDeleteFolderAsync(id, deletedBy);
                
                if (!success)
                {
                    return NotFound($"Folder with ID {id} not found");
                }

                return Ok(new { message = "Folder permanently deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting folder: {FolderId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Private Helper Methods

        private string GetAbuneIdFromToken()
        {
            return User.FindFirst("AbuneId")?.Value ?? string.Empty;
        }

        private string GetUserIdFromToken()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        #endregion
    }
}
