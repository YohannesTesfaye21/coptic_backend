# Media Upload API Documentation

## Overview

The Media Upload API provides comprehensive file upload and management capabilities for books, videos, and audio files within the Coptic App Backend. This system uses MinIO for scalable object storage and integrates with the existing folder management system.

## Features

- **Multiple Media Types**: Support for books (PDF, EPUB, MOBI, etc.), videos (MP4, AVI, MOV, etc.), and audio (MP3, WAV, OGG, etc.)
- **Folder Integration**: All media files are organized within the existing folder hierarchy
- **MinIO Storage**: Scalable object storage with presigned URLs for secure access
- **File Validation**: Comprehensive validation for file types and sizes
- **Access Control**: Restricted to Abune users only
- **File Management**: Upload, download, list, and delete operations

## Authentication

All media operations require authentication and are restricted to Abune users only. Include the JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

### Base URL
```
/api/media
```

## Media Upload Endpoints

### Upload Media File
```http
POST /api/media/upload
Content-Type: multipart/form-data

Form Data:
- file: [FILE] (required) - Media file (books, videos, or audio)
- folderId: "folder123" (required) - Target folder ID
- mediaType: "Book" (required) - Media type (Book, Video, Audio)
- description: "Optional description" (optional)
```

**Media Type Options:**
- `Book` - For PDF, EPUB, MOBI, TXT, DOC, DOCX files (max 100MB)
- `Video` - For MP4, AVI, MOV, WMV, FLV, WEBM files (max 500MB)
- `Audio` - For MP3, WAV, OGG, M4A, AAC, FLAC files (max 200MB)

**Response:**
```json
{
  "objectName": "media/folder123/book/book_name_20241201_143022_abc12345.pdf",
  "fileName": "book_name.pdf",
  "fileSize": 2048576,
  "fileType": "application/pdf",
  "mediaType": "Book",
  "folderId": "folder123",
  "fileUrl": "https://minio.example.com/presigned-url",
  "uploadedAt": "2024-12-01T14:30:22Z"
}
```

**Example Upload Requests:**

**Upload a Book:**
```http
POST /api/media/upload
Content-Type: multipart/form-data

Form Data:
- file: [FILE] - book.pdf
- folderId: "folder123"
- mediaType: "Book"
- description: "Religious text"
```

**Upload a Video:**
```http
POST /api/media/upload
Content-Type: multipart/form-data

Form Data:
- file: [FILE] - video.mp4
- folderId: "folder123"
- mediaType: "Video"
- description: "Sermon recording"
```

**Upload Audio:**
```http
POST /api/media/upload
Content-Type: multipart/form-data

Form Data:
- file: [FILE] - audio.mp3
- folderId: "folder123"
- mediaType: "Audio"
- description: "Hymn recording"
```

## Media Management Endpoints

### Get Media Files in Folder
```http
GET /api/media/folder/{folderId}?mediaType={mediaType}
```

**Parameters:**
- `folderId` (path) - Folder ID
- `mediaType` (query, optional) - Filter by media type (Book, Video, Audio)

**Response:**
```json
[
  {
    "fileName": "book_name.pdf",
    "objectName": "media/folder123/book/book_name_20241201_143022_abc12345.pdf",
    "fileSize": 2048576,
    "lastModified": "2024-12-01T14:30:22Z",
    "mediaType": "Book",
    "folderId": "folder123"
  }
]
```

### Download Media File
```http
GET /api/media/download/{objectName}
```

**Parameters:**
- `objectName` (path) - MinIO object name

**Response:** File stream

### Get Media File URL
```http
GET /api/media/url/{objectName}
```

**Parameters:**
- `objectName` (path) - MinIO object name

**Response:**
```json
{
  "url": "https://minio.example.com/presigned-url"
}
```

### Delete Media File
```http
DELETE /api/media/{objectName}
```

**Parameters:**
- `objectName` (path) - MinIO object name

**Response:**
```json
{
  "message": "Media file deleted successfully"
}
```

## Supported File Types

### Books
- **Extensions**: .pdf, .epub, .mobi, .txt, .doc, .docx
- **Content Types**: application/pdf, application/epub+zip, application/x-mobipocket-ebook, text/plain, application/msword, application/vnd.openxmlformats-officedocument.wordprocessingml.document
- **Max Size**: 100MB

### Videos
- **Extensions**: .mp4, .avi, .mov, .wmv, .flv, .webm
- **Content Types**: video/mp4, video/x-msvideo, video/quicktime, video/x-ms-wmv, video/x-flv, video/webm
- **Max Size**: 500MB

### Audio
- **Extensions**: .mp3, .wav, .ogg, .m4a, .aac, .flac
- **Content Types**: audio/mpeg, audio/wav, audio/ogg, audio/mp4, audio/aac, audio/flac
- **Max Size**: 200MB

## File Organization

Media files are organized in MinIO with the following structure:
```
media/
├── {folderId}/
│   ├── book/
│   │   └── {filename}_{timestamp}_{random}.{ext}
│   ├── video/
│   │   └── {filename}_{timestamp}_{random}.{ext}
│   └── audio/
│       └── {filename}_{timestamp}_{random}.{ext}
```

## Error Responses

### 400 Bad Request
```json
{
  "error": "No file provided"
}
```

### 403 Forbidden
```json
{
  "error": "You don't have access to this folder"
}
```

### 404 Not Found
```json
{
  "error": "Folder not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Internal server error",
  "message": "Detailed error message"
}
```

## Configuration

The MinIO service is configured in `appsettings.json`:

```json
{
  "MinIO": {
    "Endpoint": "162.243.165.212:9000",
    "AccessKey": "admin",
    "SecretKey": "your_secure_password_here",
    "UseSSL": false,
    "BucketName": "coptic-files"
  }
}
```

## Security Considerations

1. **Authentication Required**: All endpoints require valid JWT authentication
2. **Abune Only**: Only Abune users can upload and manage media files
3. **Folder Access Control**: Users can only access media in folders they own
4. **File Validation**: Strict validation of file types and sizes
5. **Presigned URLs**: Secure, time-limited access to files
6. **Unique Filenames**: Prevents conflicts and ensures file integrity

## Usage Examples

### Upload a Book
```bash
curl -X POST "https://api.example.com/api/media/upload" \
  -H "Authorization: Bearer your-jwt-token" \
  -F "file=@book.pdf" \
  -F "folderId=folder123" \
  -F "mediaType=Book" \
  -F "description=Religious text"
```

### Upload a Video
```bash
curl -X POST "https://api.example.com/api/media/upload" \
  -H "Authorization: Bearer your-jwt-token" \
  -F "file=@video.mp4" \
  -F "folderId=folder123" \
  -F "mediaType=Video" \
  -F "description=Sermon recording"
```

### Upload Audio
```bash
curl -X POST "https://api.example.com/api/media/upload" \
  -H "Authorization: Bearer your-jwt-token" \
  -F "file=@audio.mp3" \
  -F "folderId=folder123" \
  -F "mediaType=Audio" \
  -F "description=Hymn recording"
```

### Get Media Files in Folder
```bash
curl -X GET "https://api.example.com/api/media/folder/folder123?mediaType=Book" \
  -H "Authorization: Bearer your-jwt-token"
```

### Download a Media File
```bash
curl -X GET "https://api.example.com/api/media/download/media/folder123/book/book_name_20241201_143022_abc12345.pdf" \
  -H "Authorization: Bearer your-jwt-token" \
  -o downloaded_file.pdf
```

