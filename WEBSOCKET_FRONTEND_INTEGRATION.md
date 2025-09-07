# WebSocket Frontend Integration Guide

## Overview
This guide provides comprehensive documentation for frontend developers on how to integrate with the Coptic Chat Backend WebSocket system for real-time messaging functionality.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [WebSocket Connection](#websocket-connection)
3. [Authentication](#authentication)
4. [Event Handling](#event-handling)
5. [Message Types](#message-types)
6. [Complete Implementation Examples](#complete-implementation-examples)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)

---

## Prerequisites

### Required Libraries
```bash
# For React/JavaScript
npm install @microsoft/signalr

# For Flutter/Dart
dependencies:
  signalr_netcore_client: ^1.0.0

# For Vue.js
npm install @microsoft/signalr

# For Angular
npm install @microsoft/signalr
```

### Backend Endpoints
- **WebSocket URL**: `ws://162.243.165.212:5000/chatHub` (Development)
- **WebSocket URL**: `wss://162.243.165.212:5000/chatHub` (Production)
- **REST API Base**: `http://162.243.165.212:5000/api`

---

## WebSocket Connection

### Basic Connection Setup

```javascript
import * as signalR from '@microsoft/signalr';

class ChatWebSocketService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.jwtToken = null;
    }

    // Initialize connection with JWT token
    async connect(jwtToken) {
        try {
            this.jwtToken = jwtToken;
            
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("ws://162.243.165.212:5000/chatHub", {
                    accessTokenFactory: () => jwtToken,
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            
            console.log("WebSocket connected successfully");
            return true;
        } catch (error) {
            console.error("WebSocket connection failed:", error);
            return false;
        }
    }

    // Disconnect from WebSocket
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            console.log("WebSocket disconnected");
        }
    }
}
```

---

## Authentication

### JWT Token Requirements
The WebSocket connection requires a valid JWT token with the following claims:
- `UserId`: User's unique identifier
- `AbuneId`: Community/Abune identifier
- `UserType`: Either "Abune" or "Regular"

### Getting JWT Token
```javascript
// Login to get JWT token
async function login(email, password) {
    const response = await fetch('http://162.243.165.212:5000/api/Auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ email, password })
    });

    const data = await response.json();
    return data.accessToken; // Use this token for WebSocket connection
}

// Example usage
const token = await login('abune@church.com', 'abune123');
await chatService.connect(token);
```

---

## Event Handling

### Setting Up Event Listeners

```javascript
setupEventHandlers() {
    if (!this.connection) return;

    // Direct message received
    this.connection.on("ReceiveMessage", (message) => {
        console.log("Direct message received:", message);
        this.handleDirectMessage(message);
    });

    // Broadcast message received
    this.connection.on("ReceiveBroadcastMessage", (message) => {
        console.log("Broadcast message received:", message);
        this.handleBroadcastMessage(message);
    });

    // Media message received
    this.connection.on("ReceiveMediaMessage", (message) => {
        console.log("Media message received:", message);
        this.handleMediaMessage(message);
    });

    // Broadcast media message received
    this.connection.on("ReceiveBroadcastMediaMessage", (message) => {
        console.log("Broadcast media message received:", message);
        this.handleBroadcastMediaMessage(message);
    });

    // Message delivery confirmation
    this.connection.on("MessageDelivered", (messageId, recipientId) => {
        console.log("Message delivered:", messageId, "to", recipientId);
        this.handleMessageDelivered(messageId, recipientId);
    });

    // User presence events
    this.connection.on("UserOnline", (userId) => {
        console.log("User came online:", userId);
        this.handleUserOnline(userId);
    });

    this.connection.on("UserOffline", (userId) => {
        console.log("User went offline:", userId);
        this.handleUserOffline(userId);
    });

    // Typing indicators
    this.connection.on("TypingIndicator", (senderId, isTyping) => {
        console.log("Typing indicator:", senderId, isTyping);
        this.handleTypingIndicator(senderId, isTyping);
    });

    // Unread count updates
    this.connection.on("UnreadCountUpdate", (data) => {
        console.log("Unread count update:", data);
        this.handleUnreadCountUpdate(data);
    });

    this.connection.on("CommunityUnreadCountUpdate", (data) => {
        console.log("Community unread count update:", data);
        this.handleCommunityUnreadCountUpdate(data);
    });

    // Error handling
    this.connection.on("ErrorMessage", (error) => {
        console.error("WebSocket error:", error);
        this.handleError(error);
    });

    // Connection events
    this.connection.onclose((error) => {
        console.log("Connection closed:", error);
        this.isConnected = false;
        this.handleConnectionClosed(error);
    });

    this.connection.onreconnecting((error) => {
        console.log("Reconnecting:", error);
        this.handleReconnecting(error);
    });

    this.connection.onreconnected((connectionId) => {
        console.log("Reconnected:", connectionId);
        this.isConnected = true;
        this.handleReconnected(connectionId);
    });
}
```

---

## Message Types

### Message Object Structure
```javascript
// Direct Message
{
    "id": "string",                    // Unique message ID
    "senderId": "string",              // Sender's user ID
    "recipientId": "string",           // Recipient's user ID
    "abuneId": "string",               // Community/Abune ID
    "content": "string",               // Message content
    "messageType": 0,                  // 0=Text, 1=Image, 2=Document, 3=Voice
    "fileUrl": "string",               // File URL (for media messages)
    "fileName": "string",              // File name (for media messages)
    "fileSize": 0,                     // File size in bytes
    "fileType": "string",              // MIME type
    "voiceDuration": 0,                // Duration in seconds (for voice)
    "timestamp": 1757178403,           // Unix timestamp
    "isBroadcast": false,              // Whether it's a broadcast message
    "replyToMessageId": "string",      // ID of message being replied to
    "forwardedFromMessageId": "string", // ID of original message (if forwarded)
    "reactions": "{}",                 // JSON string of reactions
    "readStatus": "{}",                // JSON string of read status
    "status": 0,                       // 0=Sent, 1=Delivered, 2=Read, 3=Failed
    "isDeleted": false,                // Whether message is deleted
    "sender": { /* User object */ },   // Sender information
    "recipient": { /* User object */ }, // Recipient information
    "abune": { /* User object */ }     // Abune information
}

// Broadcast Message (same structure but recipientId is empty and isBroadcast is true)
{
    "id": "string",
    "senderId": "string",
    "recipientId": "",                 // Empty for broadcast
    "abuneId": "string",
    "content": "string",
    "messageType": 0,
    "timestamp": 1757178403,
    "isBroadcast": true,               // True for broadcast
    "sender": { /* User object */ },
    "abune": { /* User object */ }
}
```

### Message Type Constants
```javascript
const MessageType = {
    TEXT: 0,
    IMAGE: 1,
    DOCUMENT: 2,
    VOICE: 3
};

const MessageStatus = {
    SENT: 0,
    DELIVERED: 1,
    READ: 2,
    FAILED: 3
};
```

---

## Complete Implementation Examples

### React.js Implementation

```jsx
import React, { useState, useEffect, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

const ChatComponent = () => {
    const [connection, setConnection] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const [messages, setMessages] = useState([]);
    const [onlineUsers, setOnlineUsers] = useState([]);
    const [jwtToken, setJwtToken] = useState(null);
    const [unreadCounts, setUnreadCounts] = useState({});
    const [totalUnreadCount, setTotalUnreadCount] = useState(0);
    const [conversations, setConversations] = useState([]);

    // Initialize WebSocket connection
    const initializeConnection = useCallback(async (token) => {
        try {
            const newConnection = new signalR.HubConnectionBuilder()
                .withUrl("ws://162.243.165.212:5000/chatHub", {
                    accessTokenFactory: () => token,
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Set up event handlers
            newConnection.on("ReceiveMessage", (message) => {
                setMessages(prev => [...prev, { ...message, type: 'direct' }]);
            });

            newConnection.on("ReceiveBroadcastMessage", (message) => {
                setMessages(prev => [...prev, { ...message, type: 'broadcast' }]);
            });

            newConnection.on("UserOnline", (userId) => {
                setOnlineUsers(prev => [...prev, userId]);
            });

            newConnection.on("UserOffline", (userId) => {
                setOnlineUsers(prev => prev.filter(id => id !== userId));
            });

            newConnection.on("MessageDelivered", (messageId, recipientId) => {
                setMessages(prev => prev.map(msg => 
                    msg.id === messageId ? { ...msg, status: 1 } : msg
                ));
            });

            newConnection.on("UnreadCountUpdate", (data) => {
                setTotalUnreadCount(data.totalUnreadCount);
                setUnreadCounts(data.conversationUnreadCounts);
            });

            newConnection.on("CommunityUnreadCountUpdate", (data) => {
                // Handle community-wide unread count updates
                console.log("Community unread counts updated:", data);
            });

            await newConnection.start();
            setConnection(newConnection);
            setIsConnected(true);
        } catch (error) {
            console.error("Connection failed:", error);
        }
    }, []);

    // Send message via REST API (triggers WebSocket event)
    const sendMessage = async (recipientId, content) => {
        try {
            const response = await fetch('http://162.243.165.212:5000/api/Chat/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${jwtToken}`
                },
                body: JSON.stringify({
                    recipientId,
                    content,
                    messageType: 0 // Text
                })
            });

            if (response.ok) {
                const message = await response.json();
                setMessages(prev => [...prev, { ...message, type: 'sent' }]);
            }
        } catch (error) {
            console.error("Send message failed:", error);
        }
    };

    // Send broadcast message
    const sendBroadcast = async (content) => {
        try {
            const response = await fetch('http://162.243.165.212:5000/api/Chat/broadcast', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${jwtToken}`
                },
                body: JSON.stringify({
                    content,
                    messageType: 0
                })
            });

            if (response.ok) {
                const message = await response.json();
                setMessages(prev => [...prev, { ...message, type: 'broadcast' }]);
            }
        } catch (error) {
            console.error("Send broadcast failed:", error);
        }
    };

    // Load conversations with unread counts
    const loadConversations = async () => {
        try {
            const response = await fetch('http://162.243.165.212:5000/api/Chat/conversations', {
                headers: {
                    'Authorization': `Bearer ${jwtToken}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                setConversations(data.conversations);
                setTotalUnreadCount(data.totalUnreadCount);
                setUnreadCounts(data.unreadCounts);
            }
        } catch (error) {
            console.error("Load conversations failed:", error);
        }
    };

    // Mark conversation as read
    const markConversationAsRead = async (conversationId) => {
        try {
            const response = await fetch(`http://162.243.165.212:5000/api/Chat/conversations/${conversationId}/mark-read`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${jwtToken}`
                }
            });

            if (response.ok) {
                // Reload conversations to get updated unread counts
                await loadConversations();
            }
        } catch (error) {
            console.error("Mark as read failed:", error);
        }
    };

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, [connection]);

    return (
        <div>
            <div>Status: {isConnected ? 'Connected' : 'Disconnected'}</div>
            <div>Online Users: {onlineUsers.length}</div>
            <div>Total Unread: {totalUnreadCount}</div>
            
            {/* Conversations List */}
            <div>
                <h3>Conversations</h3>
                {conversations.map((conv) => (
                    <div key={conv.id} onClick={() => markConversationAsRead(conv.id)}>
                        <strong>{conv.abune?.name || conv.user?.name}</strong>
                        {conv.unreadCount > 0 && (
                            <span style={{color: 'red', marginLeft: '10px'}}>
                                ({conv.unreadCount} unread)
                            </span>
                        )}
                        <div>{conv.lastMessageContent}</div>
                    </div>
                ))}
            </div>

            {/* Messages */}
            <div>
                <h3>Messages</h3>
                {messages.map((message, index) => (
                    <div key={index}>
                        <strong>{message.sender?.name}:</strong> {message.content}
                        {message.isBroadcast && <span> (Broadcast)</span>}
                    </div>
                ))}
            </div>
        </div>
    );
};

export default ChatComponent;
```

### Flutter/Dart Implementation

```dart
import 'package:signalr_netcore_client/signalr_client.dart';

class ChatWebSocketService {
  late HubConnection _connection;
  bool _isConnected = false;
  String? _jwtToken;

  Future<bool> connect(String jwtToken) async {
    try {
      _jwtToken = jwtToken;
      
      _connection = HubConnectionBuilder()
          .withUrl(
            'ws://162.243.165.212:5000/chatHub',
            options: HttpConnectionOptions(
              accessTokenFactory: () => Future.value(jwtToken),
              skipNegotiation: true,
              transport: HttpTransportType.webSockets,
            ),
          )
          .withAutomaticReconnect([0, 2000, 10000, 30000])
          .build();

      // Set up event handlers
      _connection.on('ReceiveMessage', _handleDirectMessage);
      _connection.on('ReceiveBroadcastMessage', _handleBroadcastMessage);
      _connection.on('MessageDelivered', _handleMessageDelivered);
      _connection.on('UserOnline', _handleUserOnline);
      _connection.on('UserOffline', _handleUserOffline);
      _connection.on('TypingIndicator', _handleTypingIndicator);

      await _connection.start();
      _isConnected = true;
      return true;
    } catch (e) {
      print('Connection failed: $e');
      return false;
    }
  }

  void _handleDirectMessage(List<dynamic>? args) {
    if (args != null && args.isNotEmpty) {
      final message = args[0] as Map<String, dynamic>;
      print('Direct message received: ${message['content']}');
      // Handle direct message
    }
  }

  void _handleBroadcastMessage(List<dynamic>? args) {
    if (args != null && args.isNotEmpty) {
      final message = args[0] as Map<String, dynamic>;
      print('Broadcast message received: ${message['content']}');
      // Handle broadcast message
    }
  }

  void _handleMessageDelivered(List<dynamic>? args) {
    if (args != null && args.length >= 2) {
      final messageId = args[0] as String;
      final recipientId = args[1] as String;
      print('Message delivered: $messageId to $recipientId');
      // Handle delivery confirmation
    }
  }

  void _handleUserOnline(List<dynamic>? args) {
    if (args != null && args.isNotEmpty) {
      final userId = args[0] as String;
      print('User online: $userId');
      // Handle user online
    }
  }

  void _handleUserOffline(List<dynamic>? args) {
    if (args != null && args.isNotEmpty) {
      final userId = args[0] as String;
      print('User offline: $userId');
      // Handle user offline
    }
  }

  void _handleTypingIndicator(List<dynamic>? args) {
    if (args != null && args.length >= 2) {
      final senderId = args[0] as String;
      final isTyping = args[1] as bool;
      print('Typing indicator: $senderId is typing: $isTyping');
      // Handle typing indicator
    }
  }

  Future<void> disconnect() async {
    if (_isConnected) {
      await _connection.stop();
      _isConnected = false;
    }
  }
}
```

---

## Error Handling

### Common Error Scenarios

```javascript
class ChatWebSocketService {
    setupErrorHandling() {
        // Connection errors
        this.connection.onclose((error) => {
            if (error) {
                console.error("Connection closed with error:", error);
                this.handleConnectionError(error);
            } else {
                console.log("Connection closed normally");
            }
        });

        // Reconnection handling
        this.connection.onreconnecting((error) => {
            console.log("Attempting to reconnect...", error);
            this.handleReconnecting(error);
        });

        this.connection.onreconnected((connectionId) => {
            console.log("Reconnected successfully:", connectionId);
            this.handleReconnected(connectionId);
        });

        // Server errors
        this.connection.on("ErrorMessage", (error) => {
            console.error("Server error:", error);
            this.handleServerError(error);
        });
    }

    handleConnectionError(error) {
        // Implement retry logic, show user notification, etc.
        this.showNotification("Connection lost. Attempting to reconnect...");
    }

    handleReconnecting(error) {
        this.showNotification("Reconnecting...");
    }

    handleReconnected(connectionId) {
        this.showNotification("Reconnected successfully");
    }

    handleServerError(error) {
        this.showNotification("Server error: " + error);
    }

    handleUnreadCountUpdate(data) {
        // Update unread counts in UI
        this.updateUnreadCounts(data.totalUnreadCount, data.conversationUnreadCounts);
    }

    handleCommunityUnreadCountUpdate(data) {
        // Update community-wide unread counts
        this.updateCommunityUnreadCounts(data.userUnreadCounts);
    }
}
```

---

## Best Practices

### 1. Connection Management
```javascript
// Always check connection state before sending messages
if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
    // Send message
} else {
    console.warn("Not connected to WebSocket");
    // Handle offline scenario
}
```

### 2. Token Refresh
```javascript
// Implement token refresh mechanism
async refreshToken() {
    try {
        const response = await fetch('/api/Auth/refresh', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.jwtToken}`
            }
        });
        
        const data = await response.json();
        this.jwtToken = data.accessToken;
        
        // Reconnect with new token
        await this.disconnect();
        await this.connect(this.jwtToken);
    } catch (error) {
        console.error("Token refresh failed:", error);
        // Redirect to login
    }
}
```

### 3. Message Queue for Offline
```javascript
class MessageQueue {
    constructor() {
        this.queue = [];
    }

    addMessage(message) {
        this.queue.push({
            ...message,
            timestamp: Date.now(),
            retryCount: 0
        });
    }

    async processQueue() {
        while (this.queue.length > 0) {
            const message = this.queue[0];
            try {
                await this.sendMessage(message);
                this.queue.shift(); // Remove from queue
            } catch (error) {
                message.retryCount++;
                if (message.retryCount > 3) {
                    this.queue.shift(); // Give up after 3 retries
                }
                break; // Wait before next attempt
            }
        }
    }
}
```

### 4. Performance Optimization
```javascript
// Debounce typing indicators
const debounceTyping = debounce((recipientId) => {
    this.connection.invoke("SendTypingIndicator", recipientId, false);
}, 1000);

// Throttle message sending
const throttleSend = throttle((message) => {
    this.sendMessage(message);
}, 100);
```

---

## Testing

### Unit Testing WebSocket Events
```javascript
// Mock WebSocket connection for testing
const mockConnection = {
    on: jest.fn(),
    start: jest.fn().mockResolvedValue(),
    stop: jest.fn().mockResolvedValue(),
    invoke: jest.fn().mockResolvedValue(),
    state: signalR.HubConnectionState.Connected
};

// Test event handlers
test('should handle direct message', () => {
    const chatService = new ChatWebSocketService();
    const mockMessage = { id: '1', content: 'Hello' };
    
    chatService.setupEventHandlers();
    chatService.connection = mockConnection;
    
    // Simulate receiving a message
    const onReceiveMessage = mockConnection.on.mock.calls
        .find(call => call[0] === 'ReceiveMessage')[1];
    
    onReceiveMessage(mockMessage);
    
    expect(chatService.messages).toContain(mockMessage);
});
```

---

## Troubleshooting

### Common Issues

1. **Connection Failed (401 Unauthorized)**
   - Check JWT token validity
   - Ensure token has required claims (UserId, AbuneId)
   - Verify token is not expired

2. **Messages Not Received**
   - Verify user is in the correct community group
   - Check if recipient is online and connected
   - Ensure WebSocket connection is active

3. **Reconnection Issues**
   - Implement exponential backoff
   - Handle token refresh during reconnection
   - Clear message queue on successful reconnection

4. **Performance Issues**
   - Implement message pagination
   - Use virtual scrolling for large message lists
   - Debounce typing indicators and other frequent events

---

## API Reference

### WebSocket Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `ReceiveMessage` | `message` | Direct message received |
| `ReceiveBroadcastMessage` | `message` | Broadcast message received |
| `ReceiveMediaMessage` | `message` | Media message received |
| `ReceiveBroadcastMediaMessage` | `message` | Broadcast media message received |
| `MessageDelivered` | `messageId`, `recipientId` | Message delivery confirmation |
| `UserOnline` | `userId` | User came online |
| `UserOffline` | `userId` | User went offline |
| `TypingIndicator` | `senderId`, `isTyping` | Typing status update |
| `UnreadCountUpdate` | `data` | Unread count update for current user |
| `CommunityUnreadCountUpdate` | `data` | Community-wide unread count updates |
| `ErrorMessage` | `error` | Server error message |

### REST API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Chat/send` | Send direct message |
| POST | `/api/Chat/broadcast` | Send broadcast message |
| POST | `/api/Chat/send-media` | Send media message |
| POST | `/api/Chat/broadcast-media` | Send broadcast media message |
| GET | `/api/Chat/conversations` | Get conversations with unread counts |
| GET | `/api/Chat/conversations/{userId}/messages` | Get conversation messages |
| GET | `/api/Chat/broadcast` | Get broadcast messages |
| GET | `/api/Chat/unread-counts` | Get unread counts for all conversations |
| POST | `/api/Chat/conversations/{conversationId}/mark-read` | Mark conversation as read |

---

This documentation provides everything needed to integrate WebSocket functionality into your frontend application. The real-time messaging system will automatically deliver messages to subscribed users when they are sent via the REST API.
`