# File Storage API Documentation

## Overview
The file storage system handles uploads, downloads, and management of various file types for the chat system. Files are organized in a structured directory system based on community, user, and message context.

## Key Features
- **Structured Storage**: Files organized by `abuneId`, `senderId`, `recipientId`, and `messageType`
- **Multiple File Types**: Images, documents, and voice messages
- **Size Validation**: Configurable file size limits
- **Type Validation**: Whitelist of allowed file types
- **Automatic Cleanup**: Old files are automatically removed

## Base URL
```
https://localhost:7061/api
```

---

## 📁 File Upload Endpoints

### 1. Upload Chat File

```http
POST /api/FileUpload/chat-file
Authorization: Bearer {JWT_TOKEN}
Content-Type: multipart/form-data
```

**Form Data:**
- `file`: The file to upload (required)
- `recipientId`: ID of the message recipient (required)
- `messageType`: Type of message (required)
  - `1` = Image
  - `2` = Document
  - `3` = Voice

**File Requirements:**
- **Images**: JPG, PNG, GIF, WebP (max 10MB)
- **Documents**: PDF, DOC, DOCX, TXT (max 50MB)
- **Voice**: MP3, WAV, M4A (max 25MB)

**Success Response (200):**
```json
{
  "message": "File uploaded successfully",
  "fileUrl": "/uploads/chat/7ddcc57a-bead-4169-b141-4ad9ae246805/4ec2a7ad-2c91-4843-a5d4-69d7875d1310/7ddcc57a-bead-4169-b141-4ad9ae246805/image/2025-08-23/image-123.jpg",
  "fileName": "image-123.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg",
  "uploadedAt": "2025-08-23T18:30:00Z"
}
```

**Error Responses:**

**File Too Large (400):**
```json
{
  "error": "File size exceeds limit",
  "message": "Maximum file size is 10MB for images"
}
```

**Invalid File Type (400):**
```json
{
  "error": "Invalid file type",
  "message": "Only JPG, PNG, GIF, WebP files are allowed for images"
}
```

**Unauthorized (401):**
```json
{
  "error": "Authentication required",
  "message": "Please provide a valid JWT token"
}
```

### 2. Upload Broadcast File (Abune Only)

```http
POST /api/FileUpload/broadcast-file
Authorization: Bearer {JWT_TOKEN}
Content-Type: multipart/form-data
```

**Form Data:**
- `file`: The file to upload (required)
- `messageType`: Type of message (required)

**Success Response (200):**
```json
{
  "message": "Broadcast file uploaded successfully",
  "fileUrl": "/uploads/broadcast/7ddcc57a-bead-4169-b141-4ad9ae246805/document/2025-08-23/announcement.pdf",
  "fileName": "announcement.pdf",
  "fileSize": 2048000,
  "fileType": "application/pdf",
  "uploadedAt": "2025-08-23T18:30:00Z"
}
```

**Error Response (403):**
```json
{
  "error": "Access denied",
  "message": "Only Abune users can upload broadcast files"
}
```

---

## 📂 File Retrieval Endpoints

### 1. Get Chat Files

```http
GET /api/FileUpload/chat-files?abuneId={abuneId}&senderId={senderId}&recipientId={recipientId}&messageType={messageType}
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `abuneId`: Community Abune ID (required)
- `senderId`: Message sender ID (required)
- `recipientId`: Message recipient ID (required)
- `messageType`: Type of files to retrieve (optional)

**Success Response (200):**
```json
[
  {
    "fileName": "image-123.jpg",
    "fileUrl": "/uploads/chat/7ddcc57a-bead-4169-b141-4ad9ae246805/4ec2a7ad-2c91-4843-a5d4-69d7875d1310/7ddcc57a-bead-4169-b141-4ad9ae246805/image/2025-08-23/image-123.jpg",
    "fileSize": 1024000,
    "fileType": "image/jpeg",
    "uploadedAt": "2025-08-23T18:30:00Z",
    "messageType": 1
  }
]
```

### 2. Get Broadcast Files

```http
GET /api/FileUpload/broadcast-files?abuneId={abuneId}&messageType={messageType}
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `abuneId`: Community Abune ID (required)
- `messageType`: Type of files to retrieve (optional)

**Success Response (200):**
```json
[
  {
    "fileName": "announcement.pdf",
    "fileUrl": "/uploads/broadcast/7ddcc57a-bead-4169-b141-4ad9ae246805/document/2025-08-23/announcement.pdf",
    "fileSize": 2048000,
    "fileType": "application/pdf",
    "uploadedAt": "2025-08-23T18:30:00Z",
    "messageType": 2
  }
]
```

### 3. Get File Info

```http
GET /api/FileUpload/file-info?fileUrl={fileUrl}
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `fileUrl`: URL of the file (required)

**Success Response (200):**
```json
{
  "fileName": "image-123.jpg",
  "fileUrl": "/uploads/chat/7ddcc57a-bead-4169-b141-4ad9ae246805/4ec2a7ad-2c91-4843-a5d4-69d7875d1310/7ddcc57a-bead-4169-b141-4ad9ae246805/image/2025-08-23/image-123.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg",
  "uploadedAt": "2025-08-23T18:30:00Z",
  "exists": true
}
```

---

## 🗑️ File Management Endpoints

### 1. Delete File

```http
DELETE /api/FileUpload/delete?fileUrl={fileUrl}
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `fileUrl`: URL of the file to delete (required)

**Success Response (200):**
```json
{
  "message": "File deleted successfully"
}
```

**Error Response (404):**
```json
{
  "error": "File not found",
  "message": "The specified file does not exist"
}
```

---

## 📁 Directory Structure

Files are organized in the following structure:

```
uploads/
├── chat/
│   └── {abuneId}/
│       └── {senderId}/
│           └── {recipientId}/
│               └── {messageType}/
│                   └── {date}/
│                       └── {filename}
└── broadcast/
    └── {abuneId}/
        └── {messageType}/
            └── {date}/
                └── {filename}
```

**Example:**
```
uploads/
├── chat/
│   └── 7ddcc57a-bead-4169-b141-4ad9ae246805/
│       └── 4ec2a7ad-2c91-4843-a5d4-69d7875d1310/
│           └── 7ddcc57a-bead-4169-b141-4ad9ae246805/
│               └── image/
│                   └── 2025-08-23/
│                       └── profile-photo.jpg
└── broadcast/
    └── 7ddcc57a-bead-4169-b141-4ad9ae246805/
        └── document/
            └── 2025-08-23/
                └── weekly-announcement.pdf
```

---

## ⚙️ Configuration

The file storage system uses the following configuration from `appsettings.json`:

```json
{
  "FileStorage": {
    "BasePath": "uploads",
    "MaxFileSizeMB": {
      "Images": 10,
      "Documents": 50,
      "Voice": 25
    },
    "AllowedImageTypes": ["jpg", "jpeg", "png", "gif", "webp"],
    "AllowedDocumentTypes": ["pdf", "doc", "docx", "txt"],
    "AllowedVoiceTypes": ["mp3", "wav", "m4a"]
  }
}
```

---

## 📱 Frontend Implementation Notes

### 1. File Upload Flow
1. **Select file** → Validate type and size
2. **Show preview** → Display file information
3. **Upload file** → Call upload endpoint with progress
4. **Get file URL** → Use returned URL in chat message
5. **Handle errors** → Show appropriate error messages

### 2. File Validation
```javascript
const validateFile = (file, messageType) => {
  const maxSizes = {
    1: 10 * 1024 * 1024, // Images: 10MB
    2: 50 * 1024 * 1024, // Documents: 50MB
    3: 25 * 1024 * 1024   // Voice: 25MB
  };
  
  const allowedTypes = {
    1: ['image/jpeg', 'image/png', 'image/gif', 'image/webp'],
    2: ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'text/plain'],
    3: ['audio/mpeg', 'audio/wav', 'audio/mp4']
  };
  
  if (file.size > maxSizes[messageType]) {
    throw new Error(`File size exceeds ${maxSizes[messageType] / (1024 * 1024)}MB limit`);
  }
  
  if (!allowedTypes[messageType].includes(file.type)) {
    throw new Error('File type not allowed');
  }
  
  return true;
};
```

### 3. Upload Progress
```javascript
const uploadFile = async (file, recipientId, messageType) => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('recipientId', recipientId);
  formData.append('messageType', messageType);
  
  try {
    const response = await fetch('/api/FileUpload/chat-file', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData
    });
    
    if (!response.ok) {
      throw new Error('Upload failed');
    }
    
    const result = await response.json();
    return result.fileUrl;
  } catch (error) {
    console.error('Upload error:', error);
    throw error;
  }
};
```

### 4. File Display
```javascript
const renderFile = (fileInfo) => {
  switch (fileInfo.messageType) {
    case 1: // Image
      return <img src={fileInfo.fileUrl} alt={fileInfo.fileName} />;
    case 2: // Document
      return <a href={fileInfo.fileUrl} target="_blank">{fileInfo.fileName}</a>;
    case 3: // Voice
      return <audio controls src={fileInfo.fileUrl} />;
    default:
      return <span>{fileInfo.fileName}</span>;
  }
};
```

### 5. Error Handling
- **Network errors**: Implement retry logic
- **File size errors**: Show clear size limits
- **Type errors**: Display allowed file types
- **Permission errors**: Handle 403 responses gracefully

### 6. Performance Optimization
- **Lazy loading**: Load files only when needed
- **Caching**: Cache file URLs and metadata
- **Compression**: Compress images before upload
- **Progressive loading**: Show thumbnails for images

### 7. Security Considerations
- **File validation**: Always validate on client and server
- **Access control**: Ensure users can only access their files
- **Sanitization**: Clean file names and paths
- **Rate limiting**: Prevent abuse of upload endpoints
