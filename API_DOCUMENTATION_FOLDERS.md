# Folder Management API Documentation

## Overview

The Folder Management API provides comprehensive CRUD operations for hierarchical folder structures within the Coptic App Backend. This system allows Abune users to create, organize, and manage folders in a tree-like structure where folders can have parent-child relationships.

## Features

- **Hierarchical Structure**: Folders can have parent-child relationships, creating unlimited depth tree structures
- **Complete CRUD Operations**: Create, Read, Update, Delete operations for folders
- **Tree Navigation**: Get folder trees, breadcrumb paths, and hierarchical views
- **Search Functionality**: Search folders by name or description
- **Move Operations**: Move folders between different parents
- **Soft Delete**: Folders are soft-deleted (marked as inactive) by default
- **Permanent Delete**: Option to permanently delete folders and all children
- **Validation**: Comprehensive validation to prevent circular references and naming conflicts
- **Sorting**: Folders can be sorted within their parent directory

## Authentication

All folder operations require authentication and are restricted to Abune users only. Include the JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

### Base URL
```
/api/folder
```

## Models

### Folder
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "parentId": "string|null",
  "createdBy": "string",
  "abuneId": "string",
  "createdAt": "number",
  "lastModified": "number",
  "isActive": "boolean",
  "sortOrder": "number",
  "color": "string|null",
  "icon": "string|null"
}
```

### CreateFolderRequest
```json
{
  "name": "string",
  "description": "string|null",
  "parentId": "string|null",
  "color": "string|null",
  "icon": "string|null",
  "sortOrder": "number"
}
```

### UpdateFolderRequest
```json
{
  "name": "string",
  "description": "string|null",
  "parentId": "string|null",
  "color": "string|null",
  "icon": "string|null",
  "sortOrder": "number",
  "isActive": "boolean"
}
```

### MoveFolderRequest
```json
{
  "folderId": "string",
  "newParentId": "string|null",
  "newSortOrder": "number|null"
}
```

### FolderTreeNode
```json
{
  "id": "string",
  "name": "string",
  "description": "string|null",
  "parentId": "string|null",
  "createdBy": "string",
  "abuneId": "string",
  "createdAt": "number",
  "lastModified": "number",
  "isActive": "boolean",
  "sortOrder": "number",
  "color": "string|null",
  "icon": "string|null",
  "children": "FolderTreeNode[]",
  "childrenCount": "number",
  "totalChildrenCount": "number"
}
```

## Endpoints

### 1. Get All Folders
**GET** `/api/folder`

Get all folders for the current Abune's community.

**Response:**
```json
[
  {
    "id": "folder-1",
    "name": "Documents",
    "description": "Important documents",
    "parentId": null,
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#FF5733",
    "icon": "folder"
  }
]
```

### 2. Get Folder by ID
**GET** `/api/folder/{id}`

Get a specific folder by its ID.

**Parameters:**
- `id` (string): Folder ID

**Response:**
```json
{
  "id": "folder-1",
  "name": "Documents",
  "description": "Important documents",
  "parentId": null,
  "createdBy": "user-123",
  "abuneId": "abune-456",
  "createdAt": 1640995200,
  "lastModified": 1640995200,
  "isActive": true,
  "sortOrder": 0,
  "color": "#FF5733",
  "icon": "folder"
}
```

### 3. Get Root Folders
**GET** `/api/folder/root`

Get all root folders (folders with no parent).

**Response:**
```json
[
  {
    "id": "folder-1",
    "name": "Documents",
    "description": "Important documents",
    "parentId": null,
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#FF5733",
    "icon": "folder"
  }
]
```

### 4. Get Child Folders
**GET** `/api/folder/parent/{parentId}`

Get all child folders of a specific parent folder.

**Parameters:**
- `parentId` (string): Parent folder ID

**Response:**
```json
[
  {
    "id": "folder-2",
    "name": "Subfolder",
    "description": "A subfolder",
    "parentId": "folder-1",
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#33FF57",
    "icon": "subfolder"
  }
]
```

### 5. Get Folder Tree
**GET** `/api/folder/tree`

Get the complete hierarchical folder tree structure.

**Response:**
```json
[
  {
    "id": "folder-1",
    "name": "Documents",
    "description": "Important documents",
    "parentId": null,
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#FF5733",
    "icon": "folder",
    "children": [
      {
        "id": "folder-2",
        "name": "Subfolder",
        "description": "A subfolder",
        "parentId": "folder-1",
        "createdBy": "user-123",
        "abuneId": "abune-456",
        "createdAt": 1640995200,
        "lastModified": 1640995200,
        "isActive": true,
        "sortOrder": 0,
        "color": "#33FF57",
        "icon": "subfolder",
        "children": [],
        "childrenCount": 0,
        "totalChildrenCount": 0
      }
    ],
    "childrenCount": 1,
    "totalChildrenCount": 1
  }
]
```

### 6. Get Folder Path
**GET** `/api/folder/{id}/path`

Get the breadcrumb path from root to a specific folder.

**Parameters:**
- `id` (string): Folder ID

**Response:**
```json
[
  {
    "id": "folder-1",
    "name": "Documents",
    "description": "Important documents",
    "parentId": null,
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#FF5733",
    "icon": "folder"
  },
  {
    "id": "folder-2",
    "name": "Subfolder",
    "description": "A subfolder",
    "parentId": "folder-1",
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#33FF57",
    "icon": "subfolder"
  }
]
```

### 7. Search Folders
**GET** `/api/folder/search?searchTerm={term}`

Search folders by name or description.

**Parameters:**
- `searchTerm` (string): Search term

**Response:**
```json
[
  {
    "id": "folder-1",
    "name": "Documents",
    "description": "Important documents",
    "parentId": null,
    "createdBy": "user-123",
    "abuneId": "abune-456",
    "createdAt": 1640995200,
    "lastModified": 1640995200,
    "isActive": true,
    "sortOrder": 0,
    "color": "#FF5733",
    "icon": "folder"
  }
]
```

### 8. Create Folder
**POST** `/api/folder`

Create a new folder.

**Request Body:**
```json
{
  "name": "New Folder",
  "description": "A new folder description",
  "parentId": "folder-1",
  "color": "#FF5733",
  "icon": "folder",
  "sortOrder": 0
}
```

**Response:**
```json
{
  "id": "folder-3",
  "name": "New Folder",
  "description": "A new folder description",
  "parentId": "folder-1",
  "createdBy": "user-123",
  "abuneId": "abune-456",
  "createdAt": 1640995200,
  "lastModified": 1640995200,
  "isActive": true,
  "sortOrder": 0,
  "color": "#FF5733",
  "icon": "folder"
}
```

### 9. Update Folder
**PUT** `/api/folder/{id}`

Update an existing folder.

**Parameters:**
- `id` (string): Folder ID

**Request Body:**
```json
{
  "name": "Updated Folder",
  "description": "Updated description",
  "parentId": "folder-1",
  "color": "#33FF57",
  "icon": "updated-folder",
  "sortOrder": 1,
  "isActive": true
}
```

**Response:**
```json
{
  "id": "folder-3",
  "name": "Updated Folder",
  "description": "Updated description",
  "parentId": "folder-1",
  "createdBy": "user-123",
  "abuneId": "abune-456",
  "createdAt": 1640995200,
  "lastModified": 1640995300,
  "isActive": true,
  "sortOrder": 1,
  "color": "#33FF57",
  "icon": "updated-folder"
}
```

### 10. Move Folder
**PUT** `/api/folder/move`

Move a folder to a new parent.

**Request Body:**
```json
{
  "folderId": "folder-3",
  "newParentId": "folder-2",
  "newSortOrder": 0
}
```

**Response:**
```json
{
  "message": "Folder moved successfully"
}
```

### 11. Delete Folder (Soft Delete)
**DELETE** `/api/folder/{id}`

Soft delete a folder (marks as inactive).

**Parameters:**
- `id` (string): Folder ID

**Response:**
```json
{
  "message": "Folder deleted successfully"
}
```

### 12. Permanently Delete Folder
**DELETE** `/api/folder/{id}/permanent`

Permanently delete a folder and all its children.

**Parameters:**
- `id` (string): Folder ID

**Response:**
```json
{
  "message": "Folder permanently deleted successfully"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "message": "Folder name is required"
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "message": "Access denied - Abune only"
}
```

### 404 Not Found
```json
{
  "message": "Folder with ID folder-123 not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "Internal server error"
}
```

## Validation Rules

1. **Folder Name**: Required, maximum 255 characters
2. **Description**: Optional, maximum 1000 characters
3. **Parent Folder**: Must exist and belong to the same Abune
4. **Unique Names**: Folder names must be unique within the same parent directory
5. **Circular References**: Folders cannot be moved to their own descendants
6. **Color**: Must be a valid hex color code (7 characters including #)
7. **Icon**: Maximum 100 characters

## Business Logic

### Folder Hierarchy
- Folders can have unlimited depth
- Each folder belongs to a specific Abune
- Folders can only be moved within the same Abune's hierarchy
- Root folders have `parentId` set to `null`

### Soft Delete
- By default, folders are soft-deleted (marked as `isActive = false`)
- Soft-deleted folders are excluded from all queries
- All child folders are also soft-deleted when parent is deleted

### Permanent Delete
- Permanently deletes the folder and all its children
- This action cannot be undone
- Use with caution

### Sorting
- Folders are sorted by `sortOrder` first, then by name
- `sortOrder` can be updated to change folder position

## Usage Examples

### Creating a Folder Structure
```bash
# Create root folder
curl -X POST "https://api.copticapp.com/api/folder" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Documents",
    "description": "Main documents folder",
    "color": "#FF5733",
    "icon": "folder"
  }'

# Create subfolder
curl -X POST "https://api.copticapp.com/api/folder" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Reports",
    "description": "Monthly reports",
    "parentId": "folder-1",
    "color": "#33FF57",
    "icon": "report"
  }'
```

### Getting Folder Tree
```bash
curl -X GET "https://api.copticapp.com/api/folder/tree" \
  -H "Authorization: Bearer <token>"
```

### Moving a Folder
```bash
curl -X PUT "https://api.copticapp.com/api/folder/move" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "folderId": "folder-2",
    "newParentId": "folder-3",
    "newSortOrder": 0
  }'
```

## Notes

- All timestamps are Unix timestamps (seconds since epoch)
- The API follows RESTful conventions
- All operations require Abune-level authentication
- Folder operations are scoped to the authenticated Abune's community
- The system prevents circular references in folder hierarchies
- Folder names must be unique within the same parent directory
