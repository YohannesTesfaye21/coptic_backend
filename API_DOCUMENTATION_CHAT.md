# Chat System API Documentation

## Overview
The chat system enables communication between **Abune** (community leaders) and **Regular** users within the same community. The system supports text messages, media files, and broadcast messaging.

## Key Concepts
- **Abune → User**: Abune can send messages to any community member
- **User → Abune**: Regular users can only send messages to their Abune
- **No User-to-User**: Users cannot communicate directly with each other
- **Broadcast Messages**: Abune can send messages to all community members at once

## Base URL
```
https://localhost:7061/api
```

---

## 💬 Chat Endpoints

### 1. Send Text Message

```http
POST /api/Chat/send
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "content": "Hello, how are you today?",
  "messageType": 0
}
```

**Message Types:**
- `0` = Text
- `1` = Image
- `2` = Document
- `3` = Voice

**Success Response (200):**
```json
{
  "id": "msg-12345",
  "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "content": "Hello, how are you today?",
  "messageType": 0,
  "timestamp": 1755976581,
  "isBroadcast": false,
  "status": 0,
  "isDeleted": false
}
```

**Error Responses:**

**Unauthorized (401):**
```json
{
  "error": "Authentication failed",
  "message": "Invalid or expired token"
}
```

**Forbidden (403):**
```json
{
  "error": "Access denied",
  "message": "User can only send messages to their Abune, and Abune can only send messages to their community members"
}
```

**Bad Request (400):**
```json
{
  "error": "Invalid request",
  "message": "User information not found in token"
}
```

### 2. Send Media Message

```http
POST /api/Chat/send-media
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "fileUrl": "/uploads/chat/image-123.jpg",
  "fileName": "image-123.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg",
  "messageType": 1,
  "voiceDuration": null
}
```

**Success Response (200):**
```json
{
  "id": "msg-12346",
  "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "content": null,
  "fileUrl": "/uploads/chat/image-123.jpg",
  "fileName": "image-123.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg",
  "messageType": 1,
  "voiceDuration": null,
  "timestamp": 1755976581,
  "isBroadcast": false,
  "status": 0,
  "isDeleted": false
}
```

### 3. Send Broadcast Message (Abune Only)

```http
POST /api/Chat/broadcast
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "content": "Important announcement for all community members",
  "messageType": 0
}
```

**Success Response (200):**
```json
{
  "id": "msg-12347",
  "senderId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "recipientId": "",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "content": "Important announcement for all community members",
  "messageType": 0,
  "timestamp": 1755976581,
  "isBroadcast": true,
  "status": 0,
  "isDeleted": false
}
```

**Error Response (403):**
```json
{
  "error": "Access denied",
  "message": "Only Abune users can send broadcast messages"
}
```

### 4. Get Conversation Messages

```http
GET /api/Chat/conversation/{otherUserId}?limit=50&beforeTimestamp=1755976500
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `limit`: Number of messages to return (default: 50, max: 100)
- `beforeTimestamp`: Get messages before this timestamp (for pagination)

**Success Response (200):**
```json
[
  {
    "id": "msg-12345",
    "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
    "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "content": "Hello, how are you today?",
    "messageType": 0,
    "timestamp": 1755976581,
    "isBroadcast": false,
    "status": 0,
    "isDeleted": false
  },
  {
    "id": "msg-12344",
    "senderId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "recipientId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
    "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "content": "I'm doing well, thank you!",
    "messageType": 0,
    "timestamp": 1755976500,
    "isBroadcast": false,
    "status": 0,
    "isDeleted": false
  }
]
```

### 5. Get User Conversations

```http
GET /api/Chat/conversations
Authorization: Bearer {JWT_TOKEN}
```

**Success Response (200):**
```json
[
  {
    "id": "conv-12345",
    "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "userId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
    "lastMessageAt": 1755976581,
    "lastMessageContent": "Hello, how are you today?",
    "lastMessageType": 0,
    "unreadCount": 2,
    "isActive": true,
    "createdAt": 1755976500,
    "updatedAt": 1755976581
  }
]
```

### 6. Mark Message as Read

```http
POST /api/Chat/mark-read/{messageId}
Authorization: Bearer {JWT_TOKEN}
```

**Success Response (200):**
```json
{
  "message": "Message marked as read successfully"
}
```

### 7. Get Unread Count

```http
GET /api/Chat/unread-count
Authorization: Bearer {JWT_TOKEN}
```

**Success Response (200):**
```json
{
  "totalUnread": 5,
  "conversationCounts": {
    "conv-12345": 2,
    "conv-12346": 3
  }
}
```

### 8. Search Messages

```http
GET /api/Chat/search?q=hello&limit=20
Authorization: Bearer {JWT_TOKEN}
```

**Query Parameters:**
- `q`: Search term
- `limit`: Maximum number of results (default: 20)

**Success Response (200):**
```json
[
  {
    "id": "msg-12345",
    "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
    "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
    "content": "Hello, how are you today?",
    "messageType": 0,
    "timestamp": 1755976581,
    "isBroadcast": false,
    "status": 0,
    "isDeleted": false
  }
]
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
- `file`: The file to upload
- `recipientId`: ID of the message recipient
- `messageType`: Type of message (1=Image, 2=Document, 3=Voice)

**Success Response (200):**
```json
{
  "message": "File uploaded successfully",
  "fileUrl": "/uploads/chat/image-123.jpg",
  "fileName": "image-123.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg"
}
```

### 2. Upload Broadcast File (Abune Only)

```http
POST /api/FileUpload/broadcast-file
Authorization: Bearer {JWT_TOKEN}
Content-Type: multipart/form-data
```

**Form Data:**
- `file`: The file to upload
- `messageType`: Type of message

**Success Response (200):**
```json
{
  "message": "Broadcast file uploaded successfully",
  "fileUrl": "/uploads/broadcast/announcement.pdf",
  "fileName": "announcement.pdf",
  "fileSize": 2048000,
  "fileType": "application/pdf"
}
```

---

## 🔌 WebSocket (SignalR) Integration

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7061/chatHub")
  .build();
```

### Join Community
```javascript
await connection.invoke("JoinCommunity", abuneId);
```

### Send Message
```javascript
await connection.invoke("SendMessage", messageData);
```

### Receive Message
```javascript
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
});
```

### User Status Updates
```javascript
connection.on("UserStatusChanged", (userId, status) => {
  console.log(`User ${userId} is now ${status}`);
});
```

---

## 📱 Frontend Implementation Notes

### 1. Message Flow
1. **User types message** → Validate recipient permissions
2. **Send message** → Call `/api/Chat/send` endpoint
3. **Update UI** → Add message to conversation
4. **Handle response** → Show success/error feedback

### 2. Real-time Updates
- Connect to SignalR hub on app startup
- Listen for incoming messages
- Update conversation list in real-time
- Show typing indicators

### 3. File Handling
- Validate file types and sizes before upload
- Show upload progress
- Handle upload errors gracefully
- Cache uploaded files locally

### 4. Error Handling
- Handle network errors with retry logic
- Show appropriate error messages
- Handle permission errors (403) gracefully
- Implement offline message queuing

### 5. Performance Optimization
- Implement message pagination
- Cache conversation data
- Lazy load media files
- Optimize WebSocket reconnection

### 6. Security Considerations
- Validate file types server-side
- Sanitize message content
- Implement rate limiting
- Secure WebSocket connections
