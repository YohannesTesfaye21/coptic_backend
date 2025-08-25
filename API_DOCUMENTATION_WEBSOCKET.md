# WebSocket (SignalR) API Documentation

## Overview
The WebSocket system provides real-time communication capabilities for the chat application using SignalR. It enables instant message delivery, typing indicators, user status updates, and real-time notifications.

## Key Features
- **Real-time messaging**: Instant message delivery
- **User presence**: Track online/offline status
- **Typing indicators**: Show when users are typing
- **Community groups**: Organize users by community
- **Connection management**: Handle reconnections gracefully

## Connection URL
```
https://localhost:7061/chatHub
```

---

## 🔌 Connection Management

### 1. Establish Connection

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7061/chatHub")
  .withAutomaticReconnect()
  .build();
```

### 2. Start Connection

```javascript
try {
  await connection.start();
  console.log("SignalR Connected.");
} catch (err) {
  console.log("SignalR Connection Error: ", err);
}
```

### 3. Handle Connection Events

```javascript
// Connection established
connection.on("Connected", (connectionId) => {
  console.log("Connected with ID:", connectionId);
});

// Connection lost
connection.on("Disconnected", () => {
  console.log("Connection lost");
});

// Reconnecting
connection.on("Reconnecting", () => {
  console.log("Reconnecting...");
});

// Reconnected
connection.on("Reconnected", (connectionId) => {
  console.log("Reconnected with ID:", connectionId);
});
```

---

## 👥 Community Management

### 1. Join Community

```javascript
// Join a specific community (Abune group)
await connection.invoke("JoinCommunity", abuneId);
```

**Parameters:**
- `abuneId`: The ID of the Abune community to join

**Response:**
- Success: User joins the community group
- Error: Connection error or invalid community

### 2. Leave Community

```javascript
// Leave a specific community
await connection.invoke("LeaveCommunity", abuneId);
```

**Parameters:**
- `abuneId`: The ID of the Abune community to leave

---

## 💬 Message Handling

### 1. Send Message

```javascript
// Send a message to the hub
await connection.invoke("SendMessage", {
  senderId: "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  recipientId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  content: "Hello, how are you?",
  messageType: 0,
  timestamp: Date.now()
});
```

**Message Object Structure:**
```json
{
  "senderId": "string",
  "recipientId": "string",
  "abuneId": "string",
  "content": "string",
  "messageType": "number",
  "timestamp": "number"
}
```

### 2. Receive Message

```javascript
// Listen for incoming messages
connection.on("ReceiveMessage", (message) => {
  console.log("New message received:", message);
  // Update UI with new message
  addMessageToConversation(message);
});
```

**Message Object:**
```json
{
  "id": "msg-12345",
  "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "content": "Hello, how are you?",
  "messageType": 0,
  "timestamp": 1755976581,
  "isBroadcast": false,
  "status": 0,
  "isDeleted": false
}
```

### 3. Broadcast Message

```javascript
// Send broadcast message (Abune only)
await connection.invoke("SendBroadcastMessage", {
  senderId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  content: "Important announcement for all members",
  messageType: 0,
  timestamp: Date.now()
});
```

---

## ⌨️ Typing Indicators

### 1. Start Typing

```javascript
// Indicate that user is typing
await connection.invoke("StartTyping", {
  senderId: "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  recipientId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805"
});
```

### 2. Stop Typing

```javascript
// Indicate that user stopped typing
await connection.invoke("StopTyping", {
  senderId: "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  recipientId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805"
});
```

### 3. Receive Typing Indicators

```javascript
// Listen for typing indicators
connection.on("UserTyping", (typingInfo) => {
  console.log("User typing:", typingInfo);
  showTypingIndicator(typingInfo.senderId);
});

connection.on("UserStoppedTyping", (typingInfo) => {
  console.log("User stopped typing:", typingInfo);
  hideTypingIndicator(typingInfo.senderId);
});
```

**Typing Info Object:**
```json
{
  "senderId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "recipientId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "isTyping": true
}
```

---

## 👤 User Status Management

### 1. Update User Status

```javascript
// Update user's online status
await connection.invoke("UpdateUserStatus", {
  userId: "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  status: "online" // online, offline, away
});
```

### 2. Receive Status Updates

```javascript
// Listen for user status changes
connection.on("UserStatusChanged", (statusInfo) => {
  console.log("User status changed:", statusInfo);
  updateUserStatus(statusInfo.userId, statusInfo.status);
});
```

**Status Info Object:**
```json
{
  "userId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "status": "online",
  "timestamp": 1755976581
}
```

---

## 🔔 Notification System

### 1. Send Notification

```javascript
// Send a notification to specific users
await connection.invoke("SendNotification", {
  senderId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  recipientIds: ["4ec2a7ad-2c91-4843-a5d4-69d7875d1310"],
  abuneId: "7ddcc57a-bead-4169-b141-4ad9ae246805",
  title: "New Message",
  body: "You have a new message from Abune",
  type: "message"
});
```

### 2. Receive Notifications

```javascript
// Listen for notifications
connection.on("ReceiveNotification", (notification) => {
  console.log("Notification received:", notification);
  showNotification(notification);
});
```

**Notification Object:**
```json
{
  "id": "notif-12345",
  "senderId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "recipientIds": ["4ec2a7ad-2c91-4843-a5d4-69d7875d1310"],
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "title": "New Message",
  "body": "You have a new message from Abune",
  "type": "message",
  "timestamp": 1755976581,
  "isRead": false
}
```

---

## 📱 Frontend Implementation Guide

### 1. Complete SignalR Setup

```javascript
class ChatHub {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
  }

  async connect() {
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7061/chatHub")
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .build();

      this.setupEventHandlers();
      await this.connection.start();
      
      this.isConnected = true;
      this.reconnectAttempts = 0;
      
      // Join community after connection
      await this.joinCommunity(this.getAbuneId());
      
      console.log("SignalR Connected successfully");
    } catch (error) {
      console.error("SignalR Connection failed:", error);
      this.handleConnectionError(error);
    }
  }

  setupEventHandlers() {
    // Connection events
    this.connection.on("Connected", this.handleConnected.bind(this));
    this.connection.on("Disconnected", this.handleDisconnected.bind(this));
    this.connection.on("Reconnecting", this.handleReconnecting.bind(this));
    this.connection.on("Reconnected", this.handleReconnected.bind(this));

    // Message events
    this.connection.on("ReceiveMessage", this.handleNewMessage.bind(this));
    this.connection.on("UserTyping", this.handleUserTyping.bind(this));
    this.connection.on("UserStoppedTyping", this.handleUserStoppedTyping.bind(this));
    this.connection.on("UserStatusChanged", this.handleUserStatusChange.bind(this));
    this.connection.on("ReceiveNotification", this.handleNotification.bind(this));
  }

  async joinCommunity(abuneId) {
    try {
      await this.connection.invoke("JoinCommunity", abuneId);
      console.log(`Joined community: ${abuneId}`);
    } catch (error) {
      console.error("Failed to join community:", error);
    }
  }

  async sendMessage(messageData) {
    try {
      await this.connection.invoke("SendMessage", messageData);
      return true;
    } catch (error) {
      console.error("Failed to send message:", error);
      return false;
    }
  }

  async startTyping(typingData) {
    try {
      await this.connection.invoke("StartTyping", typingData);
    } catch (error) {
      console.error("Failed to start typing indicator:", error);
    }
  }

  async stopTyping(typingData) {
    try {
      await this.connection.invoke("StopTyping", typingData);
    } catch (error) {
      console.error("Failed to stop typing indicator:", error);
    }
  }

  // Event handlers
  handleConnected(connectionId) {
    console.log("Connected with ID:", connectionId);
    this.isConnected = true;
  }

  handleDisconnected() {
    console.log("Connection lost");
    this.isConnected = false;
  }

  handleReconnecting() {
    console.log("Reconnecting...");
  }

  handleReconnected(connectionId) {
    console.log("Reconnected with ID:", connectionId);
    this.isConnected = true;
    // Rejoin community
    this.joinCommunity(this.getAbuneId());
  }

  handleNewMessage(message) {
    console.log("New message received:", message);
    // Emit event for UI components
    this.emit('newMessage', message);
  }

  handleUserTyping(typingInfo) {
    console.log("User typing:", typingInfo);
    this.emit('userTyping', typingInfo);
  }

  handleUserStoppedTyping(typingInfo) {
    console.log("User stopped typing:", typingInfo);
    this.emit('userStoppedTyping', typingInfo);
  }

  handleUserStatusChange(statusInfo) {
    console.log("User status changed:", statusInfo);
    this.emit('userStatusChanged', statusInfo);
  }

  handleNotification(notification) {
    console.log("Notification received:", notification);
    this.emit('notification', notification);
  }

  handleConnectionError(error) {
    console.error("Connection error:", error);
    this.reconnectAttempts++;
    
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      setTimeout(() => this.connect(), 5000);
    } else {
      console.error("Max reconnection attempts reached");
    }
  }

  getAbuneId() {
    // Get AbuneId from JWT token or user context
    return localStorage.getItem('abuneId') || '';
  }

  // Event emitter methods
  emit(event, data) {
    // Implement event emission logic
    document.dispatchEvent(new CustomEvent(event, { detail: data }));
  }

  disconnect() {
    if (this.connection) {
      this.connection.stop();
      this.isConnected = false;
    }
  }
}
```

### 2. React Component Integration

```jsx
import React, { useEffect, useState } from 'react';
import { ChatHub } from './ChatHub';

const ChatComponent = () => {
  const [chatHub, setChatHub] = useState(null);
  const [messages, setMessages] = useState([]);
  const [typingUsers, setTypingUsers] = useState(new Set());
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const hub = new ChatHub();
    setChatHub(hub);

    // Connect to hub
    hub.connect();

    // Listen for events
    document.addEventListener('newMessage', handleNewMessage);
    document.addEventListener('userTyping', handleUserTyping);
    document.addEventListener('userStoppedTyping', handleUserStoppedTyping);
    document.addEventListener('userStatusChanged', handleUserStatusChange);

    return () => {
      hub.disconnect();
      document.removeEventListener('newMessage', handleNewMessage);
      document.removeEventListener('userTyping', handleUserTyping);
      document.removeEventListener('userStoppedTyping', handleUserStoppedTyping);
      document.removeEventListener('userStatusChanged', handleUserStatusChange);
    };
  }, []);

  const handleNewMessage = (event) => {
    const message = event.detail;
    setMessages(prev => [...prev, message]);
  };

  const handleUserTyping = (event) => {
    const typingInfo = event.detail;
    setTypingUsers(prev => new Set(prev).add(typingInfo.senderId));
  };

  const handleUserStoppedTyping = (event) => {
    const typingInfo = event.detail;
    setTypingUsers(prev => {
      const newSet = new Set(prev);
      newSet.delete(typingInfo.senderId);
      return newSet;
    });
  };

  const handleUserStatusChange = (event) => {
    const statusInfo = event.detail;
    // Update user status in UI
    console.log(`User ${statusInfo.userId} is now ${statusInfo.status}`);
  };

  const sendMessage = async (content, recipientId) => {
    if (!chatHub || !isConnected) return;

    const messageData = {
      senderId: getCurrentUserId(),
      recipientId,
      abuneId: getCurrentAbuneId(),
      content,
      messageType: 0,
      timestamp: Date.now()
    };

    const success = await chatHub.sendMessage(messageData);
    if (success) {
      // Message sent successfully
      console.log("Message sent");
    } else {
      // Handle error
      console.error("Failed to send message");
    }
  };

  const startTyping = (recipientId) => {
    if (!chatHub) return;

    const typingData = {
      senderId: getCurrentUserId(),
      recipientId,
      abuneId: getCurrentAbuneId()
    };

    chatHub.startTyping(typingData);
  };

  const stopTyping = (recipientId) => {
    if (!chatHub) return;

    const typingData = {
      senderId: getCurrentUserId(),
      recipientId,
      abuneId: getCurrentAbuneId()
    };

    chatHub.stopTyping(typingData);
  };

  return (
    <div className="chat-component">
      <div className="connection-status">
        Status: {isConnected ? 'Connected' : 'Disconnected'}
      </div>
      
      <div className="messages">
        {messages.map(message => (
          <div key={message.id} className="message">
            <span className="sender">{message.senderId}</span>
            <span className="content">{message.content}</span>
            <span className="timestamp">{new Date(message.timestamp).toLocaleTimeString()}</span>
          </div>
        ))}
      </div>

      {typingUsers.size > 0 && (
        <div className="typing-indicator">
          {Array.from(typingUsers).join(', ')} is typing...
        </div>
      )}

      <div className="message-input">
        <input
          type="text"
          placeholder="Type a message..."
          onKeyPress={(e) => {
            if (e.key === 'Enter') {
              sendMessage(e.target.value, getCurrentRecipientId());
              e.target.value = '';
            }
          }}
          onFocus={() => startTyping(getCurrentRecipientId())}
          onBlur={() => stopTyping(getCurrentRecipientId())}
        />
        <button onClick={() => {
          const input = document.querySelector('.message-input input');
          sendMessage(input.value, getCurrentRecipientId());
          input.value = '';
        }}>
          Send
        </button>
      </div>
    </div>
  );
};

export default ChatComponent;
```

---

## 🚨 Error Handling

### 1. Connection Errors
- Implement exponential backoff for reconnection
- Show user-friendly error messages
- Provide manual reconnection option

### 2. Message Errors
- Queue failed messages for retry
- Show delivery status to users
- Handle offline scenarios gracefully

### 3. Authentication Errors
- Redirect to login on 401 responses
- Refresh JWT tokens when needed
- Handle token expiration gracefully

---

## 🔒 Security Considerations

### 1. Authentication
- Validate JWT tokens on all hub methods
- Implement user authorization checks
- Prevent unauthorized access to communities

### 2. Rate Limiting
- Implement message rate limiting
- Prevent spam and abuse
- Monitor connection patterns

### 3. Data Validation
- Validate all incoming data
- Sanitize message content
- Prevent XSS attacks
