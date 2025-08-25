# Coptic Chat Backend - WebSocket & Read/Unread Features

## 🚀 New Features Implemented

### 1. Real-Time WebSocket Communication (SignalR)
- **SignalR Hub**: `/chatHub` endpoint for real-time messaging
- **WebSocket Support**: Full-duplex communication between clients and server
- **Connection Management**: Automatic user presence tracking and connection management

### 2. Read/Unread Message Tracking
- **Message Status**: Each message tracks `IsRead` boolean and `ReadBy` list
- **Unread Count**: Real-time unread message count per user
- **Read Receipts**: Notifications when messages are read by recipients

### 3. Enhanced Chat Functionality
- **Message Types**: Support for different message types (USER_TO_USER, ABUNE_TO_USER, ABUNE_TO_ALL, SYSTEM)
- **User Presence**: Track online/offline users
- **Typing Indicators**: Real-time typing status notifications
- **Message Broadcasting**: Support for direct messages and broadcast messages

## 🔌 API Endpoints

### New Chat Endpoints
```
GET  /api/Chat/messages/{messageId}           - Get specific message
GET  /api/Chat/unread/count/{userId}          - Get unread message count
GET  /api/Chat/unread/messages/{userId}       - Get unread messages
POST /api/Chat/messages/{messageId}/read      - Mark message as read
POST /api/Chat/messages/read-all              - Mark all messages as read
```

### WebSocket Hub Methods
```
/chatHub - SignalR Hub endpoint
```

**Client Methods (Server calls client):**
- `UserConnected(userId)` - When a user connects
- `UserDisconnected(userId)` - When a user disconnects
- `NewMessage(message)` - When a new message arrives
- `MessageSent(message)` - Confirmation of sent message
- `MessageRead(data)` - When a message is read
- `UnreadCount(data)` - Current unread count
- `OnlineUsers(users)` - List of online users
- `UserTyping(data)` - Typing indicator

**Server Methods (Client calls server):**
- `JoinChat(userId)` - Join chat room
- `SendMessage(message)` - Send a message
- `MarkMessageAsRead(messageId, userId)` - Mark message as read
- `GetUnreadCount(userId)` - Get unread count
- `GetOnlineUsers()` - Get online users list
- `Typing(userId, targetUserId, isTyping)` - Send typing indicator

## 🗄️ Database Schema Updates

### ChatMessages Table
- **Primary Key**: `id` (String)
- **GSI**: `SenderIdIndex` on `senderId`
- **GSI**: `TargetUserIdIndex` on `targetUserId`
- **Fields**:
  - `id`: Unique message identifier
  - `senderId`: Who sent the message
  - `senderName`: Display name of sender
  - `content`: Message content
  - `messageType`: Type of message
  - `targetUserId`: Who the message is for
  - `timestamp`: When message was sent
  - `isRead`: Whether message has been read
  - `readBy`: List of user IDs who have read the message

## 🧪 Testing

### HTML Test Client
- **Location**: `http://localhost:5199/` (serves `wwwroot/index.html`)
- **Features**:
  - Real-time connection status
  - Send/receive messages
  - View unread counts
  - See online users
  - Test different message types
  - Visual indicators for unread messages

### Testing Steps
1. Start the application: `dotnet run`
2. Open `http://localhost:5199/` in browser
3. Enter a User ID and click "Connect to Chat"
4. Click "Join Chat" to join the chat room
5. Send messages and see real-time updates
6. Open multiple browser tabs with different User IDs to test multi-user scenarios

## 🔧 Configuration

### SignalR Setup
```csharp
// Program.cs
builder.Services.AddSignalR();
app.MapHub<ChatHub>("/chatHub");
```

### CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});
```

### Static Files
```csharp
app.UseStaticFiles(); // Serves wwwroot/index.html
```

## 📱 Client Integration

### JavaScript SignalR Client
```javascript
// Connect to hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5199/chatHub")
    .build();

// Start connection
await connection.start();

// Join chat
await connection.invoke("JoinChat", userId);

// Send message
await connection.invoke("SendMessage", message);

// Listen for messages
connection.on("NewMessage", (message) => {
    console.log("New message:", message);
});
```

### Mobile App Integration
- Use SignalR client libraries for iOS/Android
- Implement push notifications for offline users
- Cache messages locally for offline reading

## 🚨 Error Handling

### Connection Issues
- Automatic reconnection attempts
- Graceful fallback to HTTP API calls
- Connection status indicators

### Message Failures
- Retry mechanisms for failed sends
- Error logging and user notifications
- Fallback to traditional HTTP endpoints

## 📊 Performance Considerations

### DynamoDB Optimization
- Use GSIs for efficient queries
- Implement pagination for large message lists
- Batch operations for read/unread updates

### SignalR Optimization
- Connection pooling for high user counts
- Message batching for multiple recipients
- Efficient group management

## 🔒 Security Features

### User Authentication
- User ID validation on all operations
- Connection-based user verification
- Message ownership validation

### Rate Limiting
- Message sending rate limits
- Connection attempt throttling
- API endpoint protection

## 🚀 Future Enhancements

### Planned Features
- **Message Encryption**: End-to-end encryption for sensitive messages
- **File Sharing**: Support for images, documents, and media
- **Message Search**: Full-text search across message history
- **User Groups**: Support for group chats and channels
- **Message Reactions**: Like, heart, and other reaction types
- **Message Threading**: Reply-to and conversation threading
- **Offline Support**: Message queuing and sync when online
- **Push Notifications**: FCM integration for mobile apps

### Scalability Improvements
- **Redis Backplane**: For multi-server deployments
- **Message Queuing**: RabbitMQ/Kafka for high-volume scenarios
- **CDN Integration**: For static assets and media files
- **Load Balancing**: Multiple SignalR server instances

## 📚 Resources

### Documentation
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr)
- [AWS DynamoDB Best Practices](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)
- [WebSocket Protocol](https://tools.ietf.org/html/rfc6455)

### Libraries
- **SignalR Client**: `@microsoft/signalr` (JavaScript)
- **Mobile**: SignalR client libraries for iOS/Android
- **Testing**: Postman, Insomnia for API testing

---

**Note**: This implementation provides a solid foundation for real-time chat functionality. The WebSocket implementation using SignalR ensures low-latency communication, while the read/unread tracking provides essential user experience features. The system is designed to be scalable and can be extended with additional features as needed.
