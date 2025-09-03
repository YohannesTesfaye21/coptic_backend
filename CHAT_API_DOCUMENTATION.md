# Chat API Documentation for Flutter Integration

## Overview
This document provides comprehensive API documentation for the Coptic App Chat System, including all endpoints, request/response formats, error handling, and Flutter integration examples.

## Base URL
```
Production: http://162.243.165.212:5000
Development: http://localhost:5000
```

## Authentication
All chat endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer <your_jwt_token>
```

## WebSocket Connection
SignalR Hub endpoint for real-time communication:
```
ws://162.243.165.212:5000/chatHub
```

---

## 1. Send Message

### Endpoint
```
POST /api/Chat/send
```

### Request Body
```json
{
  "recipientId": "string",
  "content": "string",
  "messageType": 0,
  "replyToMessageId": "string" // optional
}
```

### Message Types
- `0` = Text
- `1` = Image
- `2` = Document
- `3` = Voice

### Success Response (200)
```json
{
  "id": "f370c090-5f17-479e-8388-3159668c2f9b",
  "senderId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
  "recipientId": "37c118ab-8c06-43b1-9146-acfa57614b59",
  "abuneId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
  "content": "Hello, this is a test message",
  "messageType": 0,
  "fileUrl": null,
  "fileName": null,
  "fileSize": null,
  "fileType": null,
  "voiceDuration": null,
  "timestamp": 1756908352,
  "isBroadcast": false,
  "replyToMessageId": null,
  "forwardedFromMessageId": null,
  "reactions": "{}",
  "readStatus": "{}",
  "status": 0,
  "isDeleted": false,
  "deletedAt": null,
  "deletedBy": null,
  "sender": {
    "id": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
    "username": "abune@church.com",
    "email": "abune@church.com",
    "name": "Father Michael",
    "userType": 1,
    "churchName": "St. Mary Coptic Church",
    "location": "Cairo, Egypt"
  },
  "recipient": {
    "id": "37c118ab-8c06-43b1-9146-acfa57614b59",
    "username": "testuser@example.com",
    "email": "testuser@example.com",
    "name": "Test User",
    "userType": 0,
    "abuneId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df"
  }
}
```

### Error Responses

#### 400 Bad Request
```json
{
  "error": "Validation failed",
  "message": "Content cannot be empty"
}
```

#### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "User can only send messages to their Abune, and Abune can only send messages to their community members"
}
```

---

## 2. Send Broadcast Message (Abune Only)

### Endpoint
```
POST /api/Chat/broadcast
```

### Request Body
```json
{
  "content": "string",
  "messageType": 0
}
```

### Success Response (200)
```json
{
  "id": "broadcast-message-id",
  "senderId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
  "recipientId": "",
  "abuneId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
  "content": "This is a broadcast message to all community members",
  "messageType": 0,
  "isBroadcast": true,
  "timestamp": 1756908352,
  "sender": {
    "id": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
    "name": "Father Michael",
    "userType": 1
  }
}
```

### Error Responses

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "Only Abune users can send broadcast messages"
}
```

---

## 3. Send Media Message

### Endpoint
```
POST /api/Chat/send-media
```

### Request Body (Multipart Form Data)
```
recipientId: string
file: File
messageType: number (1=Image, 2=Document, 3=Voice)
voiceDuration: number (optional, for voice messages)
```

### Success Response (200)
```json
{
  "id": "media-message-id",
  "senderId": "sender-id",
  "recipientId": "recipient-id",
  "abuneId": "abune-id",
  "content": null,
  "messageType": 1,
  "fileUrl": "https://example.com/uploads/image.jpg",
  "fileName": "image.jpg",
  "fileSize": 1024000,
  "fileType": "image/jpeg",
  "voiceDuration": null,
  "timestamp": 1756908352,
  "isBroadcast": false
}
```

---

## 4. Get Conversations

### Endpoint
```
GET /api/Chat/conversations
```

### Success Response (200)
```json
[
  {
    "id": "3cd26364-195c-4e63-ad79-e6116bf81248",
    "abuneId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
    "userId": "37c118ab-8c06-43b1-9146-acfa57614b59",
    "lastMessageAt": 1756908352,
    "lastMessageContent": "Hello, this is a test message",
    "lastMessageType": 0,
    "unreadCount": 2,
    "isActive": true,
    "createdAt": 1756899617,
    "updatedAt": 1756908352,
    "user": {
      "id": "37c118ab-8c06-43b1-9146-acfa57614b59",
      "username": "testuser@example.com",
      "email": "testuser@example.com",
      "name": "Test User",
      "userType": 0,
      "abuneId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df"
    }
  }
]
```

---

## 5. Get Conversation Messages

### Endpoint
```
GET /api/Chat/conversations/{otherUserId}/messages?limit=50&beforeTimestamp=1756908352
```

### Important Note
- `{otherUserId}` should be the **User ID** of the other participant in the conversation, NOT the conversation ID
- For Regular users: use the **Abune ID** as the `otherUserId`
- For Abune users: use the **Regular User ID** as the `otherUserId`

### Query Parameters
- `limit`: Number of messages to retrieve (default: 50)
- `beforeTimestamp`: Get messages before this timestamp (for pagination)

### Success Response (200)
```json
[
  {
    "id": "message-id",
    "senderId": "sender-id",
    "recipientId": "recipient-id",
    "abuneId": "abune-id",
    "content": "Hello, this is a test message",
    "messageType": 0,
    "fileUrl": null,
    "fileName": null,
    "fileSize": null,
    "fileType": null,
    "voiceDuration": null,
    "timestamp": 1756908352,
    "isBroadcast": false,
    "replyToMessageId": null,
    "forwardedFromMessageId": null,
    "reactions": "{}",
    "readStatus": "{}",
    "status": 0,
    "isDeleted": false,
    "sender": {
      "id": "sender-id",
      "name": "Sender Name",
      "userType": 1
    },
    "recipient": {
      "id": "recipient-id",
      "name": "Recipient Name",
      "userType": 0
    }
  }
]
```

---

## 6. Mark Message as Read

### Endpoint
```
POST /api/Chat/{messageId}/read
```

### Success Response (200)
```json
{
  "message": "Message marked as read"
}
```

### Error Responses

#### 404 Not Found
```json
{
  "error": "Not found",
  "message": "Message not found"
}
```

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "User can only mark messages sent to them as read"
}
```

---

## 7. Add Reaction to Message

### Endpoint
```
POST /api/Chat/{messageId}/reactions
```

### Request Body
```json
{
  "emoji": "👍"
}
```

### Success Response (200)
```json
{
  "message": "Reaction added successfully"
}
```

---

## 8. Remove Reaction from Message

### Endpoint
```
DELETE /api/Chat/{messageId}/reactions
```

### Success Response (200)
```json
{
  "message": "Reaction removed successfully"
}
```

---

## 9. Delete Message

### Endpoint
```
DELETE /api/Chat/{messageId}
```

### Success Response (200)
```json
{
  "message": "Message deleted successfully"
}
```

### Error Responses

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "User can only delete their own messages"
}
```

---

## 10. Get Community Messages

### Endpoint
```
GET /api/Chat/community?limit=50&beforeTimestamp=1756908352
```

### Success Response (200)
```json
[
  {
    "id": "message-id",
    "senderId": "sender-id",
    "recipientId": "recipient-id",
    "abuneId": "abune-id",
    "content": "Community message content",
    "messageType": 0,
    "timestamp": 1756908352,
    "isBroadcast": false,
    "sender": {
      "id": "sender-id",
      "name": "Sender Name",
      "userType": 0
    }
  }
]
```

---

## 11. Search Messages

### Endpoint
```
GET /api/Chat/search?q=search_term&limit=20
```

### Query Parameters
- `q`: Search term
- `limit`: Number of results (default: 20)

### Success Response (200)
```json
[
  {
    "id": "message-id",
    "content": "Message containing search term",
    "timestamp": 1756908352,
    "sender": {
      "id": "sender-id",
      "name": "Sender Name"
    }
  }
]
```

---

## WebSocket Events (SignalR)

### Connection
```dart
// Flutter SignalR connection
final connection = HubConnectionBuilder()
    .withUrl('http://162.243.165.212:5000/chatHub',
        options: HttpConnectionOptions(
          accessTokenFactory: () async => 'your_jwt_token',
        ))
    .build();

await connection.start();
```

### Available Events

#### 1. Receive Message
```dart
connection.on('ReceiveMessage', (List<dynamic> args) {
  final message = ChatMessage.fromJson(args[0]);
  // Handle incoming message
});
```

#### 2. Receive Broadcast Message
```dart
connection.on('ReceiveBroadcastMessage', (List<dynamic> args) {
  final message = ChatMessage.fromJson(args[0]);
  // Handle broadcast message
});
```

#### 3. Message Read Status Update
```dart
connection.on('MessageRead', (List<dynamic> args) {
  final messageId = args[0] as String;
  final userId = args[1] as String;
  // Update message read status
});
```

#### 4. Typing Indicator
```dart
connection.on('TypingIndicator', (List<dynamic> args) {
  final userId = args[0] as String;
  final isTyping = args[1] as bool;
  // Handle typing indicator
});
```

#### 5. User Online Status
```dart
connection.on('UserOnlineStatus', (List<dynamic> args) {
  final userId = args[0] as String;
  final isOnline = args[1] as bool;
  // Handle user online status
});
```

### Sending Events

#### 1. Send Message
```dart
await connection.invoke('SendMessage', args: [
  recipientId,
  content,
  messageType,
  replyToMessageId // optional
]);
```

#### 2. Send Broadcast Message
```dart
await connection.invoke('SendBroadcastMessage', args: [
  content,
  messageType
]);
```

#### 3. Mark Message as Read
```dart
await connection.invoke('MarkMessageAsRead', args: [messageId]);
```

#### 4. Send Typing Indicator
```dart
await connection.invoke('SendTypingIndicator', args: [
  recipientId,
  isTyping
]);
```

---

## Flutter Integration Examples

### 1. HTTP Client Setup
```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class ChatApiService {
  static const String baseUrl = 'http://162.243.165.212:5000';
  String? _token;

  void setToken(String token) {
    _token = token;
  }

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $_token',
  };

  Future<Map<String, dynamic>> sendMessage({
    required String recipientId,
    required String content,
    int messageType = 0,
    String? replyToMessageId,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/Chat/send'),
      headers: _headers,
      body: jsonEncode({
        'recipientId': recipientId,
        'content': content,
        'messageType': messageType,
        if (replyToMessageId != null) 'replyToMessageId': replyToMessageId,
      }),
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to send message: ${response.body}');
    }
  }

  Future<List<dynamic>> getConversations() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/Chat/conversations'),
      headers: _headers,
    );

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to get conversations: ${response.body}');
    }
  }

  Future<List<dynamic>> getConversationMessages(
    String otherUserId, {
    int limit = 50,
    int? beforeTimestamp,
  }) async {
    final uri = Uri.parse('$baseUrl/api/Chat/conversations/$otherUserId/messages')
        .replace(queryParameters: {
      'limit': limit.toString(),
      if (beforeTimestamp != null) 'beforeTimestamp': beforeTimestamp.toString(),
    });

    final response = await http.get(uri, headers: _headers);

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Failed to get messages: ${response.body}');
    }
  }

  Future<void> markMessageAsRead(String messageId) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/Chat/$messageId/read'),
      headers: _headers,
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to mark message as read: ${response.body}');
    }
  }
}
```

### 2. SignalR Integration
```dart
import 'package:signalr_netcore_client/signalr_client.dart';

class ChatSignalRService {
  late HubConnection _connection;
  String? _token;

  void setToken(String token) {
    _token = token;
  }

  Future<void> connect() async {
    _connection = HubConnectionBuilder()
        .withUrl('http://162.243.165.212:5000/chatHub',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => _token ?? '',
            ))
        .build();

    // Set up event handlers
    _connection.on('ReceiveMessage', _onReceiveMessage);
    _connection.on('ReceiveBroadcastMessage', _onReceiveBroadcastMessage);
    _connection.on('MessageRead', _onMessageRead);
    _connection.on('TypingIndicator', _onTypingIndicator);

    await _connection.start();
  }

  void _onReceiveMessage(List<dynamic> args) {
    final message = args[0];
    // Handle incoming message
    print('Received message: $message');
  }

  void _onReceiveBroadcastMessage(List<dynamic> args) {
    final message = args[0];
    // Handle broadcast message
    print('Received broadcast: $message');
  }

  void _onMessageRead(List<dynamic> args) {
    final messageId = args[0];
    final userId = args[1];
    // Update message read status
    print('Message $messageId read by $userId');
  }

  void _onTypingIndicator(List<dynamic> args) {
    final userId = args[0];
    final isTyping = args[1];
    // Handle typing indicator
    print('User $userId is ${isTyping ? 'typing' : 'not typing'}');
  }

  Future<void> sendMessage(String recipientId, String content, int messageType) async {
    await _connection.invoke('SendMessage', args: [recipientId, content, messageType]);
  }

  Future<void> sendBroadcastMessage(String content, int messageType) async {
    await _connection.invoke('SendBroadcastMessage', args: [content, messageType]);
  }

  Future<void> markMessageAsRead(String messageId) async {
    await _connection.invoke('MarkMessageAsRead', args: [messageId]);
  }

  Future<void> sendTypingIndicator(String recipientId, bool isTyping) async {
    await _connection.invoke('SendTypingIndicator', args: [recipientId, isTyping]);
  }

  Future<void> disconnect() async {
    await _connection.stop();
  }
}
```

### 3. Error Handling
```dart
class ChatError {
  final String error;
  final String message;
  final int statusCode;

  ChatError({
    required this.error,
    required this.message,
    required this.statusCode,
  });

  factory ChatError.fromResponse(http.Response response) {
    try {
      final body = jsonDecode(response.body);
      return ChatError(
        error: body['error'] ?? 'Unknown error',
        message: body['message'] ?? 'An error occurred',
        statusCode: response.statusCode,
      );
    } catch (e) {
      return ChatError(
        error: 'Parse Error',
        message: 'Failed to parse error response',
        statusCode: response.statusCode,
      );
    }
  }
}

// Usage in try-catch blocks
try {
  final result = await chatApiService.sendMessage(
    recipientId: 'user-id',
    content: 'Hello',
  );
  // Handle success
} on http.ClientException catch (e) {
  // Network error
  print('Network error: $e');
} catch (e) {
  // Other errors
  print('Error: $e');
}
```

---

## Common Error Codes

| Status Code | Description | Common Causes |
|-------------|-------------|---------------|
| 200 | Success | Request completed successfully |
| 400 | Bad Request | Invalid request body or parameters |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User doesn't have permission for this action |
| 404 | Not Found | Resource (message, conversation) not found |
| 500 | Internal Server Error | Server-side error |

---

## Rate Limiting
- No specific rate limits are currently implemented
- Consider implementing client-side rate limiting for message sending

## Security Notes
- All endpoints require valid JWT authentication
- Users can only access messages within their community
- Abune users can send broadcast messages to their community
- Regular users can only send messages to their assigned Abune

---

## Troubleshooting

### Common Issues

#### 1. 500 Error when getting conversation messages
**Problem**: `GET /api/Chat/conversations/{conversationId}/messages` returns 500 error
**Solution**: Use the **User ID** of the other participant, not the conversation ID
- ❌ Wrong: `/api/Chat/conversations/3cd26364-195c-4e63-ad79-e6116bf81248/messages` (conversation ID)
- ✅ Correct: `/api/Chat/conversations/96adbd2a-c124-4061-8897-0f9c9eb0e1df/messages` (Abune ID)

#### 2. 403 Forbidden errors
**Problem**: "Users are not in the same community" error
**Solution**: Ensure the user is properly approved and belongs to the correct Abune community

#### 3. Missing /api prefix
**Problem**: Flutter app calling `/Chat/conversations` instead of `/api/Chat/conversations`
**Solution**: Always include the `/api` prefix in the base URL

### Flutter Integration Tips

1. **Base URL Configuration**:
   ```dart
   static const String baseUrl = 'http://162.243.165.212:5000/api';
   ```

2. **Getting the correct otherUserId**:
   ```dart
   // From conversation object, use the abuneId for regular users
   String otherUserId = conversation.abuneId; // For regular users
   // OR use the userId for abune users
   String otherUserId = conversation.userId; // For abune users
   ```

3. **Error Handling**:
   ```dart
   try {
     final messages = await chatApiService.getConversationMessages(otherUserId);
   } catch (e) {
     if (e.toString().contains('500')) {
       // Check if you're using conversation ID instead of user ID
       print('Error: Make sure you\'re using the correct user ID, not conversation ID');
     }
   }
   ```

## Testing
Use the provided endpoints with tools like Postman or curl to test the API before Flutter integration.

Example curl command:
```bash
curl -X POST "http://162.243.165.212:5000/api/Chat/send" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "user-id",
    "content": "Hello, this is a test message",
    "messageType": 0
  }'
```

Example for getting conversation messages:
```bash
curl -X GET "http://162.243.165.212:5000/api/Chat/conversations/96adbd2a-c124-4061-8897-0f9c9eb0e1df/messages?limit=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```
