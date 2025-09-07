# Flutter Chat & WebSocket Integration Flow

## 🎯 Overview
This document outlines the complete flow for integrating real-time chat functionality with the Coptic Chat Backend using Flutter and WebSocket (SignalR).

## ⚠️ Important: Current Backend Limitation
The current backend has **separate REST API and WebSocket implementations**. Messages sent via REST API are **NOT** automatically broadcast via WebSocket. You must choose one approach:

- **Use REST API**: Reliable but no real-time notifications
- **Use WebSocket**: Real-time but requires active connection

## 🏗️ Architecture Overview

### Current Implementation (Mixed Approach):
- **REST API**: Send messages, get message history, update read status
- **WebSocket**: Send messages, listen for live messages, typing indicators, user presence
- **Important Note**: REST API and WebSocket are currently **separate systems** - messages sent via REST API do NOT automatically emit WebSocket events

### Current Limitations:
- **No Real-time from REST**: Messages sent via REST API are only saved to database
- **WebSocket Required for Live Updates**: Must use WebSocket methods to get real-time notifications
- **Dual Implementation**: Both REST and WebSocket have their own message sending methods

## 📋 Prerequisites

### Backend Requirements
- **WebSocket Endpoint**: `ws://162.243.165.212:5000/chatHub`
- **Authentication**: JWT token with `UserId` and `AbuneId` claims
- **Message Types**: Text, Image, Video, Audio, Voice, Document, Location, Contact, System
- **Features**: Real-time messaging, typing indicators, user presence, message reactions

### Flutter Dependencies
```yaml
dependencies:
  signalr_netcore_client: ^1.0.0
  provider: ^6.1.1
  shared_preferences: ^2.2.2
  http: ^1.1.0
```

## 🔄 Integration Flow

### 1. Authentication Flow
```
User Login → Get JWT Token → Parse Token → Extract UserId & AbuneId
```

**Steps:**
1. User enters credentials
2. Call authentication API
3. Receive JWT token
4. Parse token to extract `UserId` and `AbuneId`
5. Store token securely for WebSocket connection

### 2. WebSocket Connection Flow
```
Initialize Service → Connect to Hub → Join Groups → Listen for Events
```

**Connection Process:**
1. **Initialize ChatService** with JWT token
2. **Connect to SignalR Hub** at `/chatHub` endpoint
3. **Auto-join groups**:
   - User's personal group (`UserId`)
   - Community group (`AbuneId`)
4. **Setup event listeners** for incoming messages

### 3. Message Flow

#### Sending Messages (Choose One Approach)

**Option A: REST API (Recommended for Reliability)**
```
User Input → Validate → Send via REST API → Save to Database → Update UI
```
- ✅ **Reliable**: Messages are saved to database
- ❌ **No Real-time**: Other users won't get live notifications
- ✅ **Offline Support**: Messages can be queued

**Option B: WebSocket (Required for Real-time)**
```
User Input → Validate → Send via WebSocket → Save to Database → Emit to Recipients → Update UI
```
- ✅ **Real-time**: Recipients get instant notifications
- ✅ **Reliable**: Messages are saved to database
- ❌ **Connection Dependent**: Requires active WebSocket connection

#### Receiving Messages (WebSocket Only)
```
WebSocket Event → Parse Message → Update State → Refresh UI → Update Unread Count
```

**Event Types:**
- `ReceiveMessage` - Direct messages
- `ReceiveMediaMessage` - Media files
- `ReceiveBroadcastMessage` - Community broadcasts
- `ReceiveBroadcastMediaMessage` - Media broadcasts

#### Message History (REST API)
```
Load Chat → Call REST API → Get Message History → Display Messages → Update UI
```

#### Read Status Update (REST API)
```
User Reads Message → Call REST API → Update Read Status → Update UI
```

#### Unread Count Management
```
Get Unread Count (REST API) → Sync with Server → Update UI Badge
New Message Received (WebSocket) → Increment Local Count → Update UI Badge
```

### 4. Real-time Features Flow

#### Typing Indicators
```
User Types → Send Typing Signal → Recipient Receives → Show Indicator → Stop Typing → Clear Indicator
```

#### User Presence
```
User Connects → Join Online List → Notify Others → User Disconnects → Remove from List
```

#### Message Status
```
Message Sent → Delivered Status → Read Status → Update UI Indicators
```

## 🏗️ Architecture Components

### 1. Service Layer
- **ChatService**: WebSocket connection management (for sending and listening)
- **MessageApiService**: REST API for sending messages and getting history
- **AuthService**: JWT token handling
- **UnreadCountService**: Track unread message counts

### 2. State Management
- **ChatProvider**: Real-time state management (WebSocket events)
- **MessageProvider**: Message list management (REST API + WebSocket updates)
- **UnreadCountProvider**: Track unread message counts
- **UserProvider**: User presence and online status

### 3. UI Components
- **ChatScreen**: Main chat interface
- **MessageBubble**: Individual message display
- **TypingIndicator**: Real-time typing status
- **OnlineStatus**: User presence indicator

## 📱 User Experience Flow

### 1. Chat Initialization
```
App Launch → Check Auth → Connect WebSocket → Load Message History (REST) → Show Chat List
```

### 2. Starting a Conversation
```
Select User → Open Chat → Load History (REST API) → Listen for Live Messages (WebSocket) → Choose Send Method
```

### 3. Real-time Interaction

**Option A: REST API Sending**
```
Type Message → Send via REST API → Save to Database → Update UI → (No real-time notification to others)
```

**Option B: WebSocket Sending**
```
Type Message → Send via WebSocket → Save to Database → Emit to Recipients → Update UI → Others get live notifications
```

### 4. Community Features
```
View Online Users → Send Broadcast (WebSocket) → Receive Live Announcements (WebSocket) → Update Unread Count
```

## 🔌 API Reference

### REST API Endpoints
| Method | Endpoint | Purpose | Parameters | Real-time |
|--------|----------|---------|------------|-----------|
| `POST` | `/api/Chat/send` | Send text message | recipientId, content, type | ❌ No |
| `POST` | `/api/Chat/send-media` | Send media file | recipientId, fileUrl, fileName, size, type | ❌ No |
| `POST` | `/api/Chat/broadcast` | Community announcement | content, type | ❌ No |
| `GET` | `/api/Chat/conversations/{userId}/messages` | Get message history | userId, page, limit | N/A |
| `POST` | `/api/Chat/{messageId}/read` | Mark as read | messageId | ❌ No |
| `GET` | `/api/Chat/unread-counts` | Get unread count | userId | N/A |

### WebSocket Events (Sending & Listening)
| Method | Purpose | Parameters | Real-time |
|--------|---------|------------|-----------|
| `SendMessage` | Send text message | recipientId, content, type, replyTo | ✅ Yes |
| `SendMediaMessage` | Send media file | recipientId, fileUrl, fileName, size, type | ✅ Yes |
| `SendBroadcastMessage` | Community announcement | content, type | ✅ Yes |
| `SendTypingIndicator` | Typing status | recipientId, isTyping | ✅ Yes |
| `MarkMessageAsRead` | Mark as read | messageId | ✅ Yes |
| `AddReaction` | Add emoji reaction | messageId, emoji | ✅ Yes |

### Server → Client (WebSocket Events)
| Event | Purpose | Data |
|-------|---------|------|
| `ReceiveMessage` | New text message | ChatMessage object |
| `ReceiveMediaMessage` | New media message | ChatMessage object |
| `ReceiveBroadcastMessage` | Community message | ChatMessage object |
| `TypingIndicator` | Someone is typing | senderId, isTyping |
| `MessageDelivered` | Message delivered | messageId, recipientId |
| `MessageRead` | Message read | messageId, userId |
| `UserOnline` | User came online | userId |
| `UserOffline` | User went offline | userId |
| `OnlineUsers` | List of online users | Array of userIds |
| `UnreadCountUpdate` | Unread count changed | userId, count |

## 🎨 UI Flow States

### 1. Connection States
- **Disconnected**: Show offline indicator
- **Connecting**: Show loading spinner
- **Connected**: Show online indicator
- **Reconnecting**: Show reconnection status

### 2. Message States
- **Sending**: Show sending indicator
- **Sent**: Show single checkmark
- **Delivered**: Show double checkmark
- **Read**: Show blue checkmarks
- **Failed**: Show retry button

### 3. Typing States
- **Not Typing**: No indicator
- **Typing**: Show "User is typing..."
- **Stopped Typing**: Clear indicator after delay

## 🔄 Error Handling Flow

### 1. Connection Errors
```
Connection Lost → Show Offline → Auto Retry → Reconnect → Restore State
```

### 2. Message Errors
```
Send Failed → Show Error → Retry Option → Success → Update UI
```

### 3. Authentication Errors
```
Token Expired → Refresh Token → Reconnect → Continue Chat
```

## 📊 Performance Considerations

### 1. Message Management
- **Pagination**: Load messages in batches
- **Caching**: Store recent messages locally
- **Cleanup**: Remove old messages from memory

### 2. Connection Management
- **Auto-reconnect**: Handle network interruptions
- **Heartbeat**: Keep connection alive
- **Background**: Pause when app is backgrounded

### 3. UI Optimization
- **Lazy Loading**: Load messages as needed
- **Debouncing**: Limit typing indicator updates
- **Memory**: Dispose unused resources

## 🚀 Implementation Steps

### Phase 1: Basic Setup
1. Add dependencies
2. Create data models
3. Setup authentication
4. Initialize WebSocket connection

### Phase 2: Core Features
1. Send/receive text messages
2. Display message history
3. Handle connection states
4. Basic error handling

### Phase 3: Advanced Features
1. Typing indicators
2. User presence
3. Message reactions
4. Media messages

### Phase 4: Polish
1. UI animations
2. Performance optimization
3. Error recovery
4. Testing

## 🔒 Security Considerations

### 1. Token Management
- Store JWT securely
- Implement token refresh
- Handle token expiration

### 2. Message Security
- Validate all inputs
- Sanitize message content
- Implement rate limiting

### 3. Connection Security
- Use secure WebSocket (WSS) in production
- Implement certificate pinning
- Validate server certificates

## 📱 Testing Strategy

### 1. Unit Tests
- Service layer methods
- State management logic
- Message parsing

### 2. Integration Tests
- WebSocket connection
- Message sending/receiving
- Authentication flow

### 3. UI Tests
- Chat screen interactions
- Message display
- Error handling

## 🎯 Success Metrics

### 1. Performance
- Message delivery time < 100ms
- Connection stability > 99%
- UI responsiveness

### 2. User Experience
- Smooth typing indicators
- Reliable message delivery
- Intuitive interface

### 3. Reliability
- Auto-reconnection works
- Error recovery is seamless
- No message loss

## 💡 Recommendations

### For Production Apps:
- **Use WebSocket for sending messages** to ensure real-time delivery
- **Use REST API for message history** and unread counts
- **Implement fallback to REST API** when WebSocket is unavailable
- **Consider hybrid approach**: Send via WebSocket, fallback to REST API

### For Simple Apps:
- **Use REST API only** if real-time is not critical
- **Use WebSocket only** if you can handle connection management

---

This flow provides a complete roadmap for implementing real-time chat functionality in Flutter with your Coptic Chat Backend! 🚀
