# API Documentation for UI Implementation

## Overview
This document provides comprehensive API documentation for the Folder and Media controllers, designed for frontend UI implementation. All GET endpoints are publicly accessible (no authentication required), while POST, PUT, and DELETE endpoints require Abune authentication.

## Authentication
- **GET endpoints**: No authentication required (public access)
- **POST/PUT/DELETE endpoints**: Require `Authorization: Bearer <JWT_TOKEN>` header
- **User Type**: Must be "Abune" for write operations

---

## 📁 Folder Controller

Base URL: `http://162.243.165.212:5000/api/Folder`

### GET Endpoints (Public Access)

#### 1. Get All Folders
```http
GET /api/Folder
```

**Description**: Retrieves all folders in hierarchical structure across all Abunes.

**Response**:
```json
[
  {
    "id": "string",
    "name": "string",
    "description": "string",
    "parentId": "string",
    "abuneId": "string",
    "path": "string",
    "isActive": true,
    "createdAt": "2025-09-13T10:30:00Z",
    "updatedAt": "2025-09-13T10:30:00Z",
    "children": [
      {
        "id": "string",
        "name": "string",
        "description": "string",
        "parentId": "string",
        "abuneId": "string",
        "path": "string",
        "isActive": true,
        "createdAt": "2025-09-13T10:30:00Z",
        "updatedAt": "2025-09-13T10:30:00Z",
        "children": []
      }
    ]
  }
]
```

#### 2. Get Folder by ID
```http
GET /api/Folder/{id}
```

**Description**: Retrieves a specific folder by its ID.

**Parameters**:
- `id` (string): Folder ID

**Response**:
```json
{
  "id": "string",
  "name": "string",
  "description": "string",
  "parentId": "string",
  "abuneId": "string",
  "path": "string",
  "isActive": true,
  "createdAt": "2025-09-13T10:30:00Z",
  "updatedAt": "2025-09-13T10:30:00Z"
}
```

#### 3. Get Root Folders
```http
GET /api/Folder/root
```

**Description**: Retrieves all root folders (folders with no parent).

**Response**: Array of Folder objects (same structure as above)

#### 4. Get Child Folders
```http
GET /api/Folder/parent/{parentId}
```

**Description**: Retrieves all child folders of a specific parent folder.

**Parameters**:
- `parentId` (string): Parent folder ID

**Response**: Array of Folder objects

#### 5. Get Folder Tree
```http
GET /api/Folder/tree
```

**Description**: Retrieves the complete folder tree structure.

**Response**: Array of FolderTreeNode objects (hierarchical structure)

#### 6. Get Folder Path
```http
GET /api/Folder/{id}/path
```

**Description**: Retrieves the breadcrumb path from root to a specific folder.

**Parameters**:
- `id` (string): Folder ID

**Response**: Array of Folder objects representing the path

#### 7. Search Folders
```http
GET /api/Folder/search?searchTerm={term}
```

**Description**: Searches folders by name or description.

**Parameters**:
- `searchTerm` (string): Search term

**Response**: Array of Folder objects matching the search term

### POST/PUT/DELETE Endpoints (Authentication Required)

#### 8. Create Folder
```http
POST /api/Folder
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "string",
  "description": "string",
  "parentId": "string" // Optional, null for root folder
}
```

**Response**: Created Folder object

#### 9. Update Folder
```http
PUT /api/Folder/{id}
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "string",
  "description": "string"
}
```

**Response**: Updated Folder object

#### 10. Move Folder
```http
PUT /api/Folder/move
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body**:
```json
{
  "folderId": "string",
  "newParentId": "string" // null to move to root
}
```

**Response**: Success message

#### 11. Delete Folder (Soft Delete)
```http
DELETE /api/Folder/{id}
Authorization: Bearer <JWT_TOKEN>
```

**Response**: Success message

#### 12. Permanently Delete Folder
```http
DELETE /api/Folder/{id}/permanent
Authorization: Bearer <JWT_TOKEN>
```

**Response**: Success message

---

## 🎵 Media Controller

Base URL: `http://162.243.165.212:5000/api/Media`

### GET Endpoints (Public Access)

#### 1. Get Media Files in Folder
```http
GET /api/Media/folder/{folderId}?mediaType={type}
```

**Description**: Retrieves all media files in a specific folder.

**Parameters**:
- `folderId` (string): Folder ID
- `mediaType` (optional): Filter by media type (0=Book, 1=Video, 2=Audio, 3=Other)

**Response**:
```json
[
  {
    "fileName": "string",
    "objectName": "string",
    "fileUrl": "http://162.243.165.212:9000/coptic-files/...",
    "fileSize": 1048576,
    "lastModified": "2025-09-13T10:30:00Z",
    "mediaType": 2,
    "folderId": "string"
  }
]
```

#### 2. Download Media File
```http
GET /api/Media/download/{objectName}
```

**Description**: Downloads a media file.

**Parameters**:
- `objectName` (string): URL-encoded object name

**Response**: File stream with appropriate content type

#### 3. Get Media File URL
```http
GET /api/Media/url/{objectName}
```

**Description**: Gets the public URL for a media file.

**Parameters**:
- `objectName` (string): Object name

**Response**:
```json
{
  "url": "http://162.243.165.212:9000/coptic-files/..."
}
```

### POST/DELETE Endpoints (Authentication Required)

#### 4. Upload Media File
```http
POST /api/Media/upload
Authorization: Bearer <JWT_TOKEN>
Content-Type: multipart/form-data
```

**Request Body** (multipart/form-data):
- `File`: The file to upload (max 5GB)
- `FolderId`: Target folder ID
- `MediaType`: Media type (0=Book, 1=Video, 2=Audio, 3=Other)
- `Description`: Optional description

**Response**:
```json
{
  "objectName": "string",
  "fileName": "string",
  "fileSize": 1048576,
  "fileType": "audio/mpeg",
  "mediaType": 2,
  "folderId": "string",
  "fileUrl": "http://162.243.165.212:9000/coptic-files/...",
  "uploadedAt": "2025-09-13T10:30:00Z"
}
```

#### 5. Delete Media File
```http
DELETE /api/Media/{objectName}
Authorization: Bearer <JWT_TOKEN>
```

**Parameters**:
- `objectName` (string): URL-encoded object name

**Response**:
```json
{
  "message": "Media file deleted successfully"
}
```

---

## 📋 Media Types

| Value | Type | Supported Formats |
|-------|------|-------------------|
| 0 | Book | PDF, EPUB, MOBI, TXT, DOC, DOCX |
| 1 | Video | MP4, AVI, MOV, WMV, FLV, WEBM |
| 2 | Audio | MP3, WAV, OGG, M4A, AAC, FLAC |
| 3 | Other | Any file type |

---

## 🔧 File Upload Configuration

- **Maximum file size**: 5GB per file
- **Request timeout**: 30 minutes for large uploads
- **Supported storage**: MinIO (primary) with local storage fallback
- **File organization**: Organized by folder structure with date-based subdirectories

---

## 🚀 Frontend Implementation Examples

### JavaScript/TypeScript Examples

#### 1. Fetch Folders
```javascript
// Get all folders
const response = await fetch('http://162.243.165.212:5000/api/Folder');
const folders = await response.json();

// Get media files in a folder
const mediaResponse = await fetch('http://162.243.165.212:5000/api/Media/folder/folder-id-here');
const mediaFiles = await mediaResponse.json();
```

#### 2. Upload File
```javascript
const uploadFile = async (file, folderId, mediaType, description = '') => {
  const formData = new FormData();
  formData.append('File', file);
  formData.append('FolderId', folderId);
  formData.append('MediaType', mediaType.toString());
  formData.append('Description', description);

  const response = await fetch('http://162.243.165.212:5000/api/Media/upload', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwtToken}`
    },
    body: formData
  });

  return await response.json();
};
```

#### 3. Download File
```javascript
const downloadFile = (objectName) => {
  const encodedName = encodeURIComponent(objectName);
  window.open(`http://162.243.165.212:5000/api/Media/download/${encodedName}`);
};
```

#### 4. Create Folder
```javascript
const createFolder = async (name, description, parentId = null) => {
  const response = await fetch('http://162.243.165.212:5000/api/Folder', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${jwtToken}`
    },
    body: JSON.stringify({
      name,
      description,
      parentId
    })
  });

  return await response.json();
};
```

### React Examples

#### 1. Folder Tree Component
```jsx
import React, { useState, useEffect } from 'react';

const FolderTree = () => {
  const [folders, setFolders] = useState([]);

  useEffect(() => {
    fetch('http://162.243.165.212:5000/api/Folder/tree')
      .then(res => res.json())
      .then(data => setFolders(data));
  }, []);

  const renderFolder = (folder) => (
    <div key={folder.id} style={{ marginLeft: '20px' }}>
      <div>{folder.name}</div>
      {folder.children?.map(renderFolder)}
    </div>
  );

  return (
    <div>
      {folders.map(renderFolder)}
    </div>
  );
};
```

#### 2. File Upload Component
```jsx
import React, { useState } from 'react';

const FileUpload = ({ folderId, onUploadComplete }) => {
  const [uploading, setUploading] = useState(false);

  const handleFileUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('File', file);
    formData.append('FolderId', folderId);
    formData.append('MediaType', '2'); // Audio
    formData.append('Description', '');

    try {
      const response = await fetch('http://162.243.165.212:5000/api/Media/upload', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('jwtToken')}`
        },
        body: formData
      });

      const result = await response.json();
      onUploadComplete(result);
    } catch (error) {
      console.error('Upload failed:', error);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <input
        type="file"
        onChange={handleFileUpload}
        disabled={uploading}
      />
      {uploading && <p>Uploading...</p>}
    </div>
  );
};
```

---

## 🔒 Error Handling

### Common HTTP Status Codes

- **200**: Success
- **201**: Created (for POST operations)
- **400**: Bad Request (invalid data)
- **401**: Unauthorized (missing or invalid token)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **500**: Internal Server Error

### Error Response Format
```json
{
  "error": "Error type",
  "message": "Detailed error message",
  "details": "Additional error details (optional)"
}
```

---

## 📝 Notes for UI Implementation

1. **File URLs**: All file URLs are permanent MinIO URLs that can be used directly in `<img>`, `<video>`, `<audio>` tags, or for downloads.

2. **Folder Hierarchy**: Use the `children` property in folder responses to build tree structures.

3. **File Size Display**: Convert `fileSize` (bytes) to human-readable format (KB, MB, GB).

4. **Media Type Icons**: Use the `mediaType` value to display appropriate icons for different file types.

5. **Search**: Implement search functionality using the `/api/Folder/search` and `/api/Media/folder/{folderId}` endpoints.

6. **Pagination**: For large datasets, consider implementing client-side pagination or request specific folder contents.

7. **Progress Tracking**: For large file uploads, consider implementing progress bars using XMLHttpRequest instead of fetch.

8. **Error Handling**: Always handle network errors and display user-friendly messages.

9. **Authentication**: Store JWT tokens securely and handle token expiration gracefully.

10. **CORS**: The API supports CORS for the specified origins in development and production environments.
