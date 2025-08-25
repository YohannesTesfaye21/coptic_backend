# 📁 FILE STORAGE SYSTEM - Images, Voice & Documents

## 📋 **Overview**

This document explains how **voice (audio)**, **images**, and **documents** are stored and managed in the chat system.

## 🏗 **Architecture**

### **Storage Components:**
1. **AWS S3** - File storage (images, audio, documents)
2. **DynamoDB** - Message metadata and file references
3. **File URLs** - Secure access to stored files

### **File Flow:**
```
Frontend → Upload API → S3 Storage → Message Creation → Real-time Broadcast
```

## 📱 **Supported File Types**

### **🖼 Images**
- **Formats**: JPEG, PNG, GIF, WebP
- **Size Limit**: 10MB (regular users), 50MB (Abune)
- **Use Cases**: Photos, screenshots, documents

### **🎵 Voice/Audio**
- **Formats**: MP3, WAV, OGG, M4A, AAC
- **Size Limit**: 10MB (regular users), 50MB (Abune)
- **Use Cases**: Voice messages, recordings

### **📄 Documents**
- **Formats**: PDF, TXT, DOC, DOCX
- **Size Limit**: 10MB (regular users), 50MB (Abune)
- **Use Cases**: Documents, forms, prayers

## 🛠 **API Endpoints**

### **📤 File Upload**
```http
POST /api/chat/messages/upload
Content-Type: multipart/form-data

Form Data:
- file: [FILE] (required)
- senderId: "user123" (required)
- targetUserId: "604c892c-4081-7074-cac6-d675a509db31" (required for regular users)
- textContent: "Optional message with file" (optional)
```

**Response:**
```json
{
  "id": "msg123",
  "senderId": "user123",
  "targetUserId": "604c892c-4081-7074-cac6-d675a509db31",
  "content": "📎 voice_message.mp3",
  "messageType": "USER_TO_ABUNE",
  "fileUrl": "https://coptic-app-chat-files.s3.amazonaws.com/chat-files/audios/2024/01/uuid.mp3",
  "fileType": "audio",
  "fileName": "voice_message.mp3",
  "fileSize": 1048576,
  "timestamp": 1704067200000
}
```

### **📥 File Download**
```http
GET /api/chat/files/download?fileUrl={fileUrl}&userId={userId}
```

**Response:**
```json
{
  "downloadUrl": "https://coptic-app-chat-files.s3.amazonaws.com/...",
  "expiresIn": "1 hour",
  "message": "Use this URL to download the file"
}
```

### **🔗 File Streaming**
```http
GET /api/chat/files/stream?fileUrl={fileUrl}&userId={userId}
```

**Response:** Direct file stream (for browser playback/preview)

### **🗑 File Deletion**
```http
DELETE /api/chat/files/{messageId}?userId={userId}
```

## 📂 **S3 Storage Structure**

```
coptic-app-chat-files/
├── chat-files/
│   ├── images/
│   │   ├── 2024/
│   │   │   ├── 01/
│   │   │   │   ├── uuid1.jpg
│   │   │   │   └── uuid2.png
│   │   │   └── 02/
│   │   └── 2023/
│   ├── audios/
│   │   ├── 2024/
│   │   │   ├── 01/
│   │   │   │   ├── uuid3.mp3
│   │   │   │   └── uuid4.wav
│   │   │   └── 02/
│   │   └── 2023/
│   └── documents/
│       ├── 2024/
│       │   ├── 01/
│       │   │   ├── uuid5.pdf
│       │   │   └── uuid6.docx
│       │   └── 02/
│       └── 2023/
```

## 🔐 **Security Features**

### **Access Control:**
- **Regular users** can only send files **TO** Abune
- **Abune** can send files to any user
- **File access** requires user authentication
- **Signed URLs** with expiration (1 hour default)

### **File Validation:**
- **Size limits** enforced
- **File type validation** by MIME type
- **Malicious file** prevention
- **Server-side encryption** (AES256)

### **Storage Security:**
- **Private S3 bucket** (not public)
- **Pre-signed URLs** for secure access
- **Automatic cleanup** of expired URLs
- **Encryption at rest**

## 💬 **Chat Integration**

### **Message Structure:**
```json
{
  "id": "msg123",
  "senderId": "user123",
  "content": "📎 Check this prayer book",
  "fileUrl": "https://s3.../prayer_book.pdf",
  "fileType": "document",
  "fileName": "prayer_book.pdf",
  "fileSize": 2048576,
  "messageType": "USER_TO_ABUNE",
  "timestamp": 1704067200000
}
```

### **Real-time Updates:**
- **File uploads** trigger WebSocket events
- **Upload progress** can be tracked
- **File messages** appear immediately in chat
- **Thumbnails** generated for images (future)

## 🌐 **WebSocket Events**

### **File Upload Events:**
```javascript
// Send file message via WebSocket
hub.invoke("SendFileMessage", {
  senderId: "user123",
  targetUserId: "604c892c-4081-7074-cac6-d675a509db31",
  fileUrl: "https://s3.../file.mp3",
  fileName: "voice_message.mp3",
  fileType: "audio",
  content: "📎 Voice message"
});

// Receive file message
hub.on("NewMessage", (message) => {
  if (message.fileUrl) {
    // Handle file message
    displayFileMessage(message);
  }
});
```

## 📱 **Frontend Implementation**

### **File Upload Component:**
```javascript
async function uploadFile(file, senderId, targetUserId, textContent) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('senderId', senderId);
  formData.append('targetUserId', targetUserId);
  if (textContent) formData.append('textContent', textContent);
  
  const response = await fetch('/api/chat/messages/upload', {
    method: 'POST',
    body: formData
  });
  
  return await response.json();
}
```

### **File Display:**
```javascript
function displayFileMessage(message) {
  switch (message.fileType) {
    case 'image':
      return `<img src="${message.fileUrl}" alt="${message.fileName}" class="chat-image" />`;
    case 'audio':
      return `<audio controls><source src="${message.fileUrl}" type="audio/mpeg"></audio>`;
    case 'document':
      return `<a href="${message.fileUrl}" target="_blank">📄 ${message.fileName}</a>`;
    default:
      return `<a href="${message.fileUrl}" download>📎 ${message.fileName}</a>`;
  }
}
```

### **Voice Recording:**
```javascript
async function recordVoice() {
  const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
  const mediaRecorder = new MediaRecorder(stream);
  const chunks = [];
  
  mediaRecorder.ondataavailable = (event) => chunks.push(event.data);
  mediaRecorder.onstop = async () => {
    const blob = new Blob(chunks, { type: 'audio/mpeg' });
    const file = new File([blob], 'voice_message.mp3', { type: 'audio/mpeg' });
    await uploadFile(file, senderId, targetUserId, "Voice message");
  };
  
  mediaRecorder.start();
  // Stop after recording...
  setTimeout(() => mediaRecorder.stop(), 5000);
}
```

## 🔧 **Configuration**

### **S3 Configuration:**
```json
{
  "AWS": {
    "Region": "eu-north-1",
    "S3": {
      "BucketName": "coptic-app-chat-files"
    }
  }
}
```

### **File Limits:**
```csharp
// Regular users: 10MB
const long REGULAR_USER_LIMIT = 10 * 1024 * 1024;

// Abune: 50MB
const long ABUNE_LIMIT = 50 * 1024 * 1024;
```

## 📊 **File Management**

### **Storage Monitoring:**
- **Bucket size** tracking
- **File count** monitoring
- **Cost optimization** strategies
- **Cleanup policies** for old files

### **File Statistics:**
```http
GET /api/chat/abune/stats
```

Returns file statistics:
```json
{
  "stats": {
    "totalFiles": 150,
    "filesByType": {
      "image": 80,
      "audio": 50,
      "document": 20
    },
    "totalStorageUsed": "2.5GB",
    "avgFileSize": "17MB"
  }
}
```

## 🚀 **Performance Optimization**

### **Upload Optimization:**
- **Chunked uploads** for large files
- **Progress tracking** for user feedback
- **Compression** for images and audio
- **Parallel uploads** for multiple files

### **Download Optimization:**
- **CDN integration** (CloudFront) for global delivery
- **Caching strategies** for frequently accessed files
- **Lazy loading** for image galleries
- **Progressive download** for audio files

## 🔮 **Future Enhancements**

### **Media Processing:**
- **Image thumbnails** generation
- **Audio transcription** for voice messages
- **Document preview** generation
- **Video support** (if needed)

### **Advanced Features:**
- **File compression** before upload
- **Virus scanning** for uploaded files
- **File deduplication** to save storage
- **Backup strategies** for important files

### **User Experience:**
- **Drag & drop** file upload
- **Paste images** from clipboard
- **File galleries** for media browsing
- **Search within documents**

## ⚡ **Quick Start**

### **1. Upload a Voice Message:**
```bash
curl -X POST "http://localhost:5199/api/chat/messages/upload" \
  -F "file=@voice_message.mp3" \
  -F "senderId=user123" \
  -F "targetUserId=604c892c-4081-7074-cac6-d675a509db31" \
  -F "textContent=Listen to this prayer"
```

### **2. Upload an Image:**
```bash
curl -X POST "http://localhost:5199/api/chat/messages/upload" \
  -F "file=@church_photo.jpg" \
  -F "senderId=user123" \
  -F "targetUserId=604c892c-4081-7074-cac6-d675a509db31"
```

### **3. Download a File:**
```bash
curl "http://localhost:5199/api/chat/files/download?fileUrl=https://s3.../file.mp3&userId=user123"
```

## 📞 **Support**

For file storage issues:
1. Check **S3 bucket permissions**
2. Verify **AWS credentials**
3. Monitor **upload file sizes**
4. Check **CORS configuration** for uploads
5. Review **server logs** for detailed errors

---

**Your chat system now supports rich media communication with secure file storage! 🎉**
