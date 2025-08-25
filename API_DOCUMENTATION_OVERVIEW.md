# Coptic App Backend API Documentation - Overview

## 🎯 **System Overview**

The Coptic App Backend is a hierarchical community communication platform that enables **Abune** (community leaders) to manage and communicate with **Regular** users within their communities. The system provides real-time chat, file sharing, and user management capabilities.

## 🏗️ **Architecture**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   API Layer     │    │   Database      │
│   (Mobile/Web)  │◄──►│   (.NET 8)      │◄──►│   (PostgreSQL)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   SignalR Hub   │    │   File Storage  │    │   JWT Auth      │
│   (Real-time)   │    │   (Local FS)    │    │   (Local)       │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 👥 **User Types & Hierarchy**

### **Abune Users (UserType = 1)**
- **Role**: Community leaders, church administrators
- **Permissions**: 
  - Send messages to any community member
  - Send broadcast messages to all members
  - Approve new user registrations
  - Manage community members
  - Upload broadcast files

### **Regular Users (UserType = 0)**
- **Role**: Community members, church attendees
- **Permissions**:
  - Send messages only to their Abune
  - Receive messages from their Abune
  - Receive broadcast messages
  - Upload files for chat messages

## 🔄 **Core Workflows**

### 1. **User Registration & Approval Flow**

```mermaid
graph TD
    A[User Registers] --> B{User Type?}
    B -->|Regular| C[Account Pending Approval]
    B -->|Abune| D[Account Active]
    C --> E[Abune Reviews]
    E --> F{Approved?}
    F -->|Yes| G[Account Active]
    F -->|No| H[Account Suspended]
    G --> I[Can Login & Chat]
    H --> J[Contact Abune]
```

**Steps:**
1. **User registers** with email, password, and basic info
2. **System assigns** user to Abune based on community
3. **Regular users** require approval from their Abune
4. **Abune users** are automatically approved
5. **Approved users** can login and access chat features

### 2. **Chat Communication Flow**

```mermaid
graph TD
    A[User Sends Message] --> B{User Type?}
    B -->|Regular| C[Can send to Abune only]
    B -->|Abune| D[Can send to any community member]
    C --> E[Message Stored]
    D --> E
    E --> F[SignalR Broadcast]
    F --> G[Recipient Receives]
    G --> H[Update UI]
    H --> I[Mark as Read]
```

**Rules:**
- **Regular → Abune**: ✅ Allowed
- **Abune → Regular**: ✅ Allowed
- **Regular → Regular**: ❌ Not allowed
- **Abune → Abune**: ❌ Not allowed

### 3. **File Upload Flow**

```mermaid
graph TD
    A[User Selects File] --> B[Validate File Type & Size]
    B --> C{Valid?}
    C -->|No| D[Show Error]
    C -->|Yes| E[Upload to Server]
    E --> F[Store in Structured Directory]
    F --> G[Return File URL]
    G --> H[Use URL in Chat Message]
```

## 🌐 **API Base URLs**

| Environment | Base URL | Description |
|-------------|----------|-------------|
| **Development** | `https://localhost:7061/api` | Local development server |
| **Production** | `https://your-domain.com/api` | Production server |

## 🔐 **Authentication**

### **JWT Token Structure**
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "user-id",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "user@email.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "username",
  "UserType": "Regular|Abune",
  "UserId": "user-guid",
  "AbuneId": "abune-guid",
  "exp": 1755976581,
  "iss": "coptic-app-backend",
  "aud": "coptic-app-frontend"
}
```

### **Token Usage**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### **Token Expiration**
- **Lifetime**: 1 hour (3600 seconds)
- **Refresh**: Manual re-authentication required
- **Storage**: Secure client-side storage

## 📱 **Real-time Communication**

### **SignalR Hub**
- **URL**: `https://localhost:7061/chatHub`
- **Features**: Real-time messaging, typing indicators, user status
- **Groups**: Community-based organization
- **Reconnection**: Automatic with exponential backoff

### **WebSocket Events**
- `ReceiveMessage`: New message received
- `UserTyping`: User started typing
- `UserStoppedTyping`: User stopped typing
- `UserStatusChanged`: User online/offline status
- `ReceiveNotification`: System notifications

## 📁 **File Storage System**

### **Directory Structure**
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

### **File Type Support**
| Type | Extensions | Max Size | Use Case |
|------|------------|----------|----------|
| **Images** | JPG, PNG, GIF, WebP | 10MB | Profile photos, chat images |
| **Documents** | PDF, DOC, DOCX, TXT | 50MB | Announcements, forms |
| **Voice** | MP3, WAV, M4A | 25MB | Voice messages |

## 🚀 **Getting Started**

### 1. **Environment Setup**
```bash
# Install .NET 8.0 SDK
# Install PostgreSQL
# Configure connection strings
# Set up file storage directories
```

### 2. **Authentication Flow**
```javascript
// 1. Register user
const registerResponse = await fetch('/api/Auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(registerData)
});

// 2. Login to get token
const loginResponse = await fetch('/api/Auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(loginData)
});

const { accessToken } = await loginResponse.json();

// 3. Use token for authenticated requests
const headers = {
  'Authorization': `Bearer ${accessToken}`,
  'Content-Type': 'application/json'
};
```

### 3. **Real-time Connection**
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7061/chatHub")
  .withAutomaticReconnect()
  .build();

await connection.start();
await connection.invoke("JoinCommunity", abuneId);
```

## 📊 **Data Models**

### **User Model**
```json
{
  "id": "user-guid",
  "username": "john_doe",
  "email": "john@example.com",
  "phoneNumber": "+1234567890",
  "name": "John Doe",
  "gender": "Male",
  "deviceToken": "fcm-token",
  "userType": 0,
  "userStatus": 0,
  "abuneId": "abune-guid",
  "isApproved": true,
  "createdAt": 1755976581
}
```

### **Chat Message Model**
```json
{
  "id": "message-guid",
  "senderId": "sender-guid",
  "recipientId": "recipient-guid",
  "abuneId": "abune-guid",
  "content": "Message content",
  "messageType": 0,
  "timestamp": 1755976581,
  "isBroadcast": false,
  "status": 0
}
```

### **Chat Conversation Model**
```json
{
  "id": "conversation-guid",
  "abuneId": "abune-guid",
  "userId": "user-guid",
  "lastMessageAt": 1755976581,
  "lastMessageContent": "Last message",
  "lastMessageType": 0,
  "unreadCount": 5,
  "isActive": true
}
```

## 🔒 **Security & Authorization**

### **Authorization Policies**
- **`AbuneOnly`**: Restricted to Abune users
- **`RegularUserOnly`**: Restricted to Regular users
- **`AuthenticatedUser`**: Any authenticated user

### **Access Control**
- Users can only access their own data
- Abune can access community member data
- File access restricted to community members
- JWT token validation on all endpoints

### **Rate Limiting**
- Message sending limits
- File upload restrictions
- Connection throttling
- API call frequency limits

## 📱 **Mobile App Integration**

### **Push Notifications**
- **FCM Integration**: Device token management
- **Notification Types**: New messages, approvals, broadcasts
- **Background Processing**: Handle notifications when app is closed

### **Offline Support**
- **Message Queuing**: Queue messages when offline
- **Local Storage**: Cache conversations and files
- **Sync on Reconnect**: Sync pending operations

### **Performance Optimization**
- **Lazy Loading**: Load messages on demand
- **Image Caching**: Cache profile pictures and chat images
- **Connection Management**: Optimize SignalR reconnection

## 🧪 **Testing & Development**

### **Swagger Documentation**
- **URL**: `https://localhost:7061/swagger`
- **Features**: Interactive API testing
- **Authentication**: JWT token support
- **Request/Response**: Full schema documentation

### **Development Tools**
- **Postman Collections**: Pre-configured API requests
- **Database Tools**: Direct database access for testing
- **Logging**: Comprehensive application logging
- **Error Handling**: Detailed error messages and codes

## 🚨 **Error Handling**

### **HTTP Status Codes**
- **200**: Success
- **201**: Created
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (authentication required)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **500**: Internal Server Error

### **Error Response Format**
```json
{
  "error": "Error type",
  "message": "Human-readable error description",
  "details": "Additional error information (optional)"
}
```

### **Common Error Scenarios**
- **Invalid JWT**: Redirect to login
- **Insufficient Permissions**: Show access denied message
- **Validation Errors**: Display field-specific errors
- **Network Errors**: Implement retry logic

## 📈 **Monitoring & Analytics**

### **Performance Metrics**
- **Response Times**: API endpoint performance
- **Connection Counts**: Active SignalR connections
- **File Upload Success**: Upload success rates
- **Error Rates**: API error frequency

### **User Analytics**
- **Active Users**: Daily/monthly active users
- **Message Volume**: Chat activity metrics
- **File Usage**: Storage and bandwidth usage
- **Feature Adoption**: Usage patterns

## 🔄 **Deployment & DevOps**

### **Environment Configuration**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=coptic_app;Username=coptic_user;Password=password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "coptic-app-backend",
    "Audience": "coptic-app-frontend",
    "ExpirationHours": 1
  },
  "FileStorage": {
    "BasePath": "uploads",
    "MaxFileSizeMB": {
      "Images": 10,
      "Documents": 50,
      "Voice": 25
    }
  }
}
```

### **Deployment Checklist**
- [ ] Database migrations applied
- [ ] Environment variables configured
- [ ] File storage directories created
- [ ] SSL certificates installed
- [ ] Firewall rules configured
- [ ] Monitoring tools deployed

## 📚 **Additional Resources**

### **Documentation Files**
- `API_DOCUMENTATION_AUTH.md` - Authentication & User Management
- `API_DOCUMENTATION_CHAT.md` - Chat System
- `API_DOCUMENTATION_FILES.md` - File Storage
- `API_DOCUMENTATION_WEBSOCKET.md` - WebSocket/SignalR

### **Code Examples**
- **Frontend SDK**: JavaScript/TypeScript client library
- **Mobile SDK**: React Native integration examples
- **Backend SDK**: .NET client examples
- **Testing**: Postman collections and test scripts

### **Support & Community**
- **GitHub Repository**: Source code and issues
- **Documentation**: API reference and guides
- **Support**: Technical support and troubleshooting
- **Updates**: Release notes and changelog
