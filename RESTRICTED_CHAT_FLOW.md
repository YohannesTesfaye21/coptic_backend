# 🔒 RESTRICTED CHAT FLOW - ABUNE ONLY SYSTEM

## 📋 **Overview**

This system implements a **restricted chat flow** where:
- **Regular users** can **ONLY** send messages **TO** Abune
- **Regular users** can **ONLY** receive messages **FROM** Abune  
- **Abune** can send messages to **any specific user** or **broadcast to all users**
- **Abune** can see **ALL messages** (both sent and received)

## 🆔 **Static Abune ID**

```
ABUNE_ID = "604c892c-4081-7074-cac6-d675a509db31"
```

## 🔐 **Chat Restrictions**

### **For Regular Users:**
- ✅ **CAN** send messages **TO** Abune
- ✅ **CAN** receive messages **FROM** Abune
- ❌ **CANNOT** send messages to other users
- ❌ **CANNOT** receive messages from other users
- ❌ **CANNOT** see conversations between other users and Abune

### **For Abune:**
- ✅ **CAN** send messages to **any specific user**
- ✅ **CAN** broadcast messages to **all users**
- ✅ **CAN** receive messages from **any user**
- ✅ **CAN** see **ALL conversations** and messages
- ✅ **CAN** view statistics and user lists

## 📱 **Message Types**

| Message Type | Description | Who Can Send | Who Can Receive |
|--------------|-------------|--------------|-----------------|
| `USER_TO_ABUNE` | Regular user → Abune | Regular Users | Abune Only |
| `ABUNE_TO_USER` | Abune → Specific User | Abune Only | Specific User |
| `ABUNE_TO_ALL` | Abune → All Users | Abune Only | All Users |
| `SYSTEM` | System messages | System | All Users |

## 🌐 **WebSocket Events (SignalR)**

### **Connection Events:**
```javascript
// Join chat with user role enforcement
hub.invoke("JoinChat", userId, userName)

// Send message (automatically restricted)
hub.invoke("SendMessage", message)

// Real-time events
hub.on("MessageSent", (message) => { /* Message sent successfully */ })
hub.on("NewMessage", (message) => { /* New message received */ })
hub.on("MessageError", (error) => { /* Error message */ })
hub.on("UnreadCount", (data) => { /* Updated unread count */ })
```

### **Real-time Restrictions:**
- **Regular users** get `MessageError` if trying to send to non-Abune users
- **Message types** are automatically enforced
- **Unread counts** are filtered by user role

## 🛠 **REST API Endpoints**

### **Regular User Endpoints:**
```http
# Send message to Abune (only)
POST /api/chat/messages
{
  "senderId": "user123",
  "targetUserId": "604c892c-4081-7074-cac6-d675a509db31",
  "content": "Hello Abune"
}

# Get messages (only Abune conversations)
GET /api/chat/messages?userId=user123&limit=50

# Get unread count
GET /api/chat/unread/count/user123

# Mark message as read
POST /api/chat/messages/{messageId}/read
{
  "userId": "user123"
}
```

### **Abune-Only Endpoints:**
```http
# Get all conversations
GET /api/chat/abune/conversations

# Get conversation with specific user
GET /api/chat/abune/conversation/{userId}

# Get Abune statistics
GET /api/chat/abune/stats

# Get all users who chatted with Abune
GET /api/chat/abune/users

# Send broadcast message
POST /api/chat/messages/broadcast
{
  "senderId": "604c892c-4081-7074-cac6-d675a509db31",
  "senderName": "Abune",
  "content": "Important announcement",
  "targetUserIds": ["user1", "user2", "user3"]
}

# Send message to specific user
POST /api/chat/messages
{
  "senderId": "604c892c-4081-7074-cac6-d675a509db31",
  "targetUserId": "user123",
  "content": "Personal message"
}
```

## 🔍 **Message Filtering Examples**

### **Regular User (user123) View:**
```json
{
  "messages": [
    {
      "id": "msg1",
      "senderId": "user123",
      "targetUserId": "604c892c-4081-7074-cac6-d675a509db31",
      "content": "Hello Abune",
      "messageType": "USER_TO_ABUNE"
    },
    {
      "id": "msg2", 
      "senderId": "604c892c-4081-7074-cac6-d675a509db31",
      "targetUserId": "user123",
      "content": "Hello my child",
      "messageType": "ABUNE_TO_USER"
    }
  ]
}
```

### **Abune View (All Messages):**
```json
{
  "messages": [
    {
      "id": "msg1",
      "senderId": "user123",
      "targetUserId": "604c892c-4081-7074-cac6-d675a509db31", 
      "content": "Hello Abune",
      "messageType": "USER_TO_ABUNE"
    },
    {
      "id": "msg2",
      "senderId": "user456", 
      "targetUserId": "604c892c-4081-7074-cac6-d675a509db31",
      "content": "Prayer request",
      "messageType": "USER_TO_ABUNE"
    },
    {
      "id": "msg3",
      "senderId": "604c892c-4081-7074-cac6-d675a509db31",
      "targetUserId": "user123",
      "content": "Hello my child",
      "messageType": "ABUNE_TO_USER"
    }
  ]
}
```

## 🚫 **Security Features**

### **Automatic Enforcement:**
- **Message sending** is restricted at both WebSocket and REST API levels
- **Message retrieval** is filtered based on user role
- **Message types** are automatically set based on sender
- **Search results** are filtered by user permissions

### **Error Handling:**
```json
{
  "error": "Regular users can only send messages to Abune"
}
```

### **Validation:**
- **Sender ID** must match authenticated user
- **Target user** is validated against restrictions
- **Message types** are enforced automatically
- **File uploads** respect user restrictions

## 📊 **Abune Dashboard Features**

### **Conversation Overview:**
- List of all users who have chatted with Abune
- Last message from each user
- Unread message counts
- Message statistics

### **User Management:**
- View all active conversations
- See user message history
- Track user engagement
- Monitor message patterns

### **Broadcast Capabilities:**
- Send messages to multiple users individually
- Track delivery and read status
- Manage broadcast lists
- Schedule announcements

## 🔧 **Implementation Details**

### **Service Layer:**
- `ChatService.PostMessageAsync()` - Enforces sending restrictions
- `ChatService.GetMessagesAsync()` - Filters messages by user role
- `ChatService.SearchMessagesAsync()` - Restricts search scope

### **WebSocket Layer:**
- `ChatHub.SendMessage()` - Real-time restriction enforcement
- `ChatHub.JoinChat()` - User role assignment
- Automatic message type setting

### **API Layer:**
- **Regular endpoints** - Filtered by user role
- **Abune endpoints** - Full access to all data
- **Broadcast endpoints** - Abune-only access

## 🧪 **Testing Scenarios**

### **Test Case 1: Regular User Sends to Abune**
```javascript
// ✅ Should work
hub.invoke("SendMessage", {
  senderId: "user123",
  targetUserId: "604c892c-4081-7074-cac6-d675a509db31",
  content: "Hello Abune"
});
```

### **Test Case 2: Regular User Sends to Another User**
```javascript
// ❌ Should fail
hub.invoke("SendMessage", {
  senderId: "user123", 
  targetUserId: "user456", // Not Abune
  content: "Hello friend"
});
// Result: MessageError "Regular users can only send messages to Abune"
```

### **Test Case 3: Abune Sends to User**
```javascript
// ✅ Should work
hub.invoke("SendMessage", {
  senderId: "604c892c-4081-7074-cac6-d675a509db31",
  targetUserId: "user123",
  content: "Hello my child"
});
```

### **Test Case 4: Abune Broadcasts**
```javascript
// ✅ Should work
hub.invoke("SendMessage", {
  senderId: "604c892c-4081-7074-cac6-d675a509db31",
  targetUserId: null, // Broadcast
  content: "Important announcement"
});
```

## 📱 **Frontend Integration**

### **User Interface:**
- **Regular users** see only Abune conversation
- **Abune** sees all conversations in a dashboard
- **Real-time updates** respect user permissions
- **Error messages** guide users on restrictions

### **Message Composition:**
- **Regular users** - Target field locked to Abune
- **Abune** - Can select any user or broadcast
- **Message types** - Automatically set based on context

### **Conversation List:**
- **Regular users** - Single conversation with Abune
- **Abune** - List of all user conversations
- **Search** - Filtered by user permissions

## 🎯 **Benefits**

1. **Security** - Users cannot communicate with each other
2. **Privacy** - Conversations are isolated to Abune
3. **Control** - Abune manages all communications
4. **Scalability** - Easy to add more restrictions
5. **Audit** - Complete message history for Abune
6. **Compliance** - Enforced at multiple levels

## 🔮 **Future Enhancements**

- **User roles** - Multiple Abune levels
- **Message approval** - Abune reviews before delivery
- **Scheduled messages** - Time-based broadcasts
- **Message templates** - Predefined responses
- **Analytics** - Advanced conversation insights
- **Moderation** - Content filtering and approval

---

**This system ensures that all communication flows through Abune, maintaining the spiritual and organizational structure of your Coptic community.**
