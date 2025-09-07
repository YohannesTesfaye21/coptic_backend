# Flutter WebSocket Chat Integration Guide

## 🚀 Overview

This guide provides complete Flutter integration for the Coptic Chat Backend WebSocket service using SignalR. The backend provides real-time messaging, user presence, typing indicators, and message status tracking.

## 📦 Dependencies

Add these dependencies to your `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  signalr_netcore_client: ^1.0.0  # SignalR client for Flutter
  http: ^1.1.0                    # For HTTP API calls
  shared_preferences: ^2.2.2     # For storing JWT tokens
  provider: ^6.1.1               # State management
  dio: ^5.4.0                    # HTTP client with interceptors
  json_annotation: ^4.8.1        # JSON serialization
  freezed_annotation: ^2.4.1     # Code generation
  uuid: ^4.2.1                   # UUID generation

dev_dependencies:
  flutter_test:
    sdk: flutter
  build_runner: ^2.4.7           # Code generation
  json_serializable: ^6.7.1      # JSON serialization
  freezed: ^2.4.6                # Code generation
```

## 🔧 Setup

### 1. Install Dependencies
```bash
flutter pub get
```

### 2. Generate Code
```bash
flutter packages pub run build_runner build
```

## 📱 Models

### ChatMessage Model
```dart
import 'package:freezed_annotation/freezed_annotation.dart';

part 'chat_message.freezed.dart';
part 'chat_message.g.dart';

@freezed
class ChatMessage with _$ChatMessage {
  const factory ChatMessage({
    required String id,
    required String senderId,
    required String recipientId,
    required String abuneId,
    required String content,
    @Default(MessageType.text) MessageType messageType,
    String? replyToMessageId,
    String? forwardedFromMessageId,
    String? fileUrl,
    String? fileName,
    int? fileSize,
    String? fileType,
    int? voiceDuration,
    @Default(false) bool isBroadcast,
    @Default(false) bool isDeleted,
    String? deletedBy,
    required int timestamp,
    @Default(MessageStatus.sent) MessageStatus status,
    Map<String, dynamic>? reactions,
    Map<String, dynamic>? readStatus,
  }) = _ChatMessage;

  factory ChatMessage.fromJson(Map<String, dynamic> json) =>
      _$ChatMessageFromJson(json);
}

enum MessageType {
  @JsonValue(0)
  text,
  @JsonValue(1)
  image,
  @JsonValue(2)
  video,
  @JsonValue(3)
  audio,
  @JsonValue(4)
  document,
  @JsonValue(5)
  voice,
  @JsonValue(6)
  location,
  @JsonValue(7)
  contact,
  @JsonValue(8)
  system,
}

enum MessageStatus {
  @JsonValue(0)
  sent,
  @JsonValue(1)
  delivered,
  @JsonValue(2)
  read,
  @JsonValue(3)
  failed,
}
```

### User Model
```dart
@freezed
class User with _$User {
  const factory User({
    required String id,
    required String username,
    required String email,
    String? phoneNumber,
    String? name,
    String? gender,
    String? deviceToken,
    @Default(false) bool emailVerified,
    @Default(false) bool phoneNumberVerified,
    required int createdAt,
    required int lastModified,
    @Default(UserType.regular) UserType userType,
    @Default(UserStatus.pendingApproval) UserStatus userStatus,
    String? abuneId,
    String? churchName,
    String? location,
    String? profileImageUrl,
    String? bio,
    @Default(false) bool isApproved,
    int? approvedAt,
    String? approvedBy,
  }) = _User;

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);
}

enum UserType {
  @JsonValue(0)
  regular,
  @JsonValue(1)
  abune,
}

enum UserStatus {
  @JsonValue(0)
  active,
  @JsonValue(1)
  inactive,
  @JsonValue(2)
  suspended,
  @JsonValue(3)
  pendingApproval,
}
```

## 🔌 SignalR Service

### ChatService Class
```dart
import 'package:signalr_netcore_client/signalr_client.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'dart:convert';

class ChatService {
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal();

  HubConnection? _connection;
  String? _jwtToken;
  String? _userId;
  String? _abuneId;

  // Connection state
  bool get isConnected => _connection?.state == HubConnectionState.Connected;
  HubConnectionState get connectionState => _connection?.state ?? HubConnectionState.Disconnected;

  // Event callbacks
  Function(ChatMessage)? onMessageReceived;
  Function(ChatMessage)? onMediaMessageReceived;
  Function(ChatMessage)? onBroadcastMessageReceived;
  Function(ChatMessage)? onBroadcastMediaMessageReceived;
  Function(String, bool)? onTypingIndicator;
  Function(String, String)? onMessageDelivered;
  Function(String, String)? onMessageRead;
  Function(String, String, String)? onReactionAdded;
  Function(String)? onUserOnline;
  Function(String)? onUserOffline;
  Function(List<String>)? onOnlineUsers;
  Function(String)? onError;

  /// Initialize the chat service with JWT token
  Future<void> initialize(String jwtToken) async {
    _jwtToken = jwtToken;
    await _parseJwtToken();
    await _initializeConnection();
  }

  /// Parse JWT token to extract user information
  Future<void> _parseJwtToken() async {
    if (_jwtToken == null) return;

    try {
      final parts = _jwtToken!.split('.');
      if (parts.length != 3) throw Exception('Invalid JWT token');

      final payload = parts[1];
      final normalized = base64Url.normalize(payload);
      final resp = utf8.decode(base64Url.decode(normalized));
      final payloadMap = json.decode(resp);

      _userId = payloadMap['UserId'];
      _abuneId = payloadMap['AbuneId'];
    } catch (e) {
      throw Exception('Failed to parse JWT token: $e');
    }
  }

  /// Initialize SignalR connection
  Future<void> _initializeConnection() async {
    if (_jwtToken == null || _userId == null || _abuneId == null) {
      throw Exception('JWT token, userId, or abuneId is missing');
    }

    _connection = HubConnectionBuilder()
        .withUrl(
          'ws://162.243.165.212:5000/chatHub',
          options: HttpConnectionOptions(
            accessTokenFactory: () => Future.value(_jwtToken!),
            skipNegotiation: true,
            transport: HttpTransportType.webSockets,
          ),
        )
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .build();

    _setupEventHandlers();
  }

  /// Setup event handlers for incoming messages
  void _setupEventHandlers() {
    _connection?.on('ReceiveMessage', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final message = ChatMessage.fromJson(args[0] as Map<String, dynamic>);
        onMessageReceived?.call(message);
      }
    });

    _connection?.on('ReceiveMediaMessage', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final message = ChatMessage.fromJson(args[0] as Map<String, dynamic>);
        onMediaMessageReceived?.call(message);
      }
    });

    _connection?.on('ReceiveBroadcastMessage', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final message = ChatMessage.fromJson(args[0] as Map<String, dynamic>);
        onBroadcastMessageReceived?.call(message);
      }
    });

    _connection?.on('ReceiveBroadcastMediaMessage', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final message = ChatMessage.fromJson(args[0] as Map<String, dynamic>);
        onBroadcastMediaMessageReceived?.call(message);
      }
    });

    _connection?.on('TypingIndicator', (List<dynamic>? args) {
      if (args != null && args.length >= 2) {
        final senderId = args[0] as String;
        final isTyping = args[1] as bool;
        onTypingIndicator?.call(senderId, isTyping);
      }
    });

    _connection?.on('MessageDelivered', (List<dynamic>? args) {
      if (args != null && args.length >= 2) {
        final messageId = args[0] as String;
        final recipientId = args[1] as String;
        onMessageDelivered?.call(messageId, recipientId);
      }
    });

    _connection?.on('MessageRead', (List<dynamic>? args) {
      if (args != null && args.length >= 2) {
        final messageId = args[0] as String;
        final userId = args[1] as String;
        onMessageRead?.call(messageId, userId);
      }
    });

    _connection?.on('ReactionAdded', (List<dynamic>? args) {
      if (args != null && args.length >= 3) {
        final messageId = args[0] as String;
        final userId = args[1] as String;
        final emoji = args[2] as String;
        onReactionAdded?.call(messageId, userId, emoji);
      }
    });

    _connection?.on('UserOnline', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final userId = args[0] as String;
        onUserOnline?.call(userId);
      }
    });

    _connection?.on('UserOffline', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final userId = args[0] as String;
        onUserOffline?.call(userId);
      }
    });

    _connection?.on('OnlineUsers', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final users = (args[0] as List).cast<String>();
        onOnlineUsers?.call(users);
      }
    });

    _connection?.on('ErrorMessage', (List<dynamic>? args) {
      if (args != null && args.isNotEmpty) {
        final error = args[0] as String;
        onError?.call(error);
      }
    });

    // Connection state handlers
    _connection?.onclose((error) {
      print('Connection closed: $error');
    });

    _connection?.onreconnecting((error) {
      print('Reconnecting: $error');
    });

    _connection?.onreconnected((connectionId) {
      print('Reconnected: $connectionId');
    });
  }

  /// Connect to the chat hub
  Future<void> connect() async {
    if (_connection == null) {
      throw Exception('Chat service not initialized');
    }

    try {
      await _connection!.start();
      print('Connected to chat hub');
    } catch (e) {
      print('Failed to connect: $e');
      rethrow;
    }
  }

  /// Disconnect from the chat hub
  Future<void> disconnect() async {
    await _connection?.stop();
    print('Disconnected from chat hub');
  }

  /// Send a text message
  Future<void> sendMessage({
    required String recipientId,
    required String content,
    MessageType messageType = MessageType.text,
    String? replyToMessageId,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('SendMessage', args: [
        recipientId,
        content,
        messageType.index,
        replyToMessageId,
      ]);
    } catch (e) {
      print('Failed to send message: $e');
      rethrow;
    }
  }

  /// Send a media message
  Future<void> sendMediaMessage({
    required String recipientId,
    required String fileUrl,
    required String fileName,
    required int fileSize,
    required String fileType,
    required MessageType messageType,
    int? voiceDuration,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('SendMediaMessage', args: [
        recipientId,
        fileUrl,
        fileName,
        fileSize,
        fileType,
        messageType.index,
        voiceDuration,
      ]);
    } catch (e) {
      print('Failed to send media message: $e');
      rethrow;
    }
  }

  /// Send a broadcast message to community
  Future<void> sendBroadcastMessage({
    required String content,
    MessageType messageType = MessageType.text,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('SendBroadcastMessage', args: [
        content,
        messageType.index,
      ]);
    } catch (e) {
      print('Failed to send broadcast message: $e');
      rethrow;
    }
  }

  /// Send a broadcast media message to community
  Future<void> sendBroadcastMediaMessage({
    required String fileUrl,
    required String fileName,
    required int fileSize,
    required String fileType,
    required MessageType messageType,
    int? voiceDuration,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('SendBroadcastMediaMessage', args: [
        fileUrl,
        fileName,
        fileSize,
        fileType,
        messageType.index,
        voiceDuration,
      ]);
    } catch (e) {
      print('Failed to send broadcast media message: $e');
      rethrow;
    }
  }

  /// Send typing indicator
  Future<void> sendTypingIndicator({
    required String recipientId,
    required bool isTyping,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('SendTypingIndicator', args: [
        recipientId,
        isTyping,
      ]);
    } catch (e) {
      print('Failed to send typing indicator: $e');
      rethrow;
    }
  }

  /// Mark message as read
  Future<void> markMessageAsRead(String messageId) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('MarkMessageAsRead', args: [messageId]);
    } catch (e) {
      print('Failed to mark message as read: $e');
      rethrow;
    }
  }

  /// Add reaction to message
  Future<void> addReaction({
    required String messageId,
    required String emoji,
  }) async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('AddReaction', args: [messageId, emoji]);
    } catch (e) {
      print('Failed to add reaction: $e');
      rethrow;
    }
  }

  /// Get online users
  Future<void> getOnlineUsers() async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('GetOnlineUsers');
    } catch (e) {
      print('Failed to get online users: $e');
      rethrow;
    }
  }

  /// Update last seen
  Future<void> updateLastSeen() async {
    if (!isConnected) throw Exception('Not connected to chat hub');

    try {
      await _connection!.invoke('UpdateLastSeen');
    } catch (e) {
      print('Failed to update last seen: $e');
      rethrow;
    }
  }
}
```

## 🏗️ State Management

### ChatProvider
```dart
import 'package:flutter/foundation.dart';
import 'package:provider/provider.dart';

class ChatProvider extends ChangeNotifier {
  final ChatService _chatService = ChatService();
  
  List<ChatMessage> _messages = [];
  List<String> _onlineUsers = [];
  Map<String, bool> _typingUsers = {};
  bool _isConnected = false;
  String? _error;

  // Getters
  List<ChatMessage> get messages => _messages;
  List<String> get onlineUsers => _onlineUsers;
  Map<String, bool> get typingUsers => _typingUsers;
  bool get isConnected => _isConnected;
  String? get error => _error;

  /// Initialize chat service
  Future<void> initialize(String jwtToken) async {
    try {
      _chatService.onMessageReceived = _onMessageReceived;
      _chatService.onMediaMessageReceived = _onMessageReceived;
      _chatService.onBroadcastMessageReceived = _onMessageReceived;
      _chatService.onBroadcastMediaMessageReceived = _onMessageReceived;
      _chatService.onTypingIndicator = _onTypingIndicator;
      _chatService.onMessageDelivered = _onMessageDelivered;
      _chatService.onMessageRead = _onMessageRead;
      _chatService.onReactionAdded = _onReactionAdded;
      _chatService.onUserOnline = _onUserOnline;
      _chatService.onUserOffline = _onUserOffline;
      _chatService.onOnlineUsers = _onOnlineUsers;
      _chatService.onError = _onError;

      await _chatService.initialize(jwtToken);
      await _chatService.connect();
      
      _isConnected = true;
      _error = null;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Send message
  Future<void> sendMessage({
    required String recipientId,
    required String content,
    MessageType messageType = MessageType.text,
    String? replyToMessageId,
  }) async {
    try {
      await _chatService.sendMessage(
        recipientId: recipientId,
        content: content,
        messageType: messageType,
        replyToMessageId: replyToMessageId,
      );
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Send media message
  Future<void> sendMediaMessage({
    required String recipientId,
    required String fileUrl,
    required String fileName,
    required int fileSize,
    required String fileType,
    required MessageType messageType,
    int? voiceDuration,
  }) async {
    try {
      await _chatService.sendMediaMessage(
        recipientId: recipientId,
        fileUrl: fileUrl,
        fileName: fileName,
        fileSize: fileSize,
        fileType: fileType,
        messageType: messageType,
        voiceDuration: voiceDuration,
      );
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Send broadcast message
  Future<void> sendBroadcastMessage({
    required String content,
    MessageType messageType = MessageType.text,
  }) async {
    try {
      await _chatService.sendBroadcastMessage(
        content: content,
        messageType: messageType,
      );
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Send typing indicator
  Future<void> sendTypingIndicator({
    required String recipientId,
    required bool isTyping,
  }) async {
    try {
      await _chatService.sendTypingIndicator(
        recipientId: recipientId,
        isTyping: isTyping,
      );
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Mark message as read
  Future<void> markMessageAsRead(String messageId) async {
    try {
      await _chatService.markMessageAsRead(messageId);
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Add reaction
  Future<void> addReaction({
    required String messageId,
    required String emoji,
  }) async {
    try {
      await _chatService.addReaction(
        messageId: messageId,
        emoji: emoji,
      );
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Get online users
  Future<void> getOnlineUsers() async {
    try {
      await _chatService.getOnlineUsers();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Update last seen
  Future<void> updateLastSeen() async {
    try {
      await _chatService.updateLastSeen();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  // Event handlers
  void _onMessageReceived(ChatMessage message) {
    _messages.add(message);
    notifyListeners();
  }

  void _onTypingIndicator(String senderId, bool isTyping) {
    _typingUsers[senderId] = isTyping;
    notifyListeners();
  }

  void _onMessageDelivered(String messageId, String recipientId) {
    // Update message status
    final index = _messages.indexWhere((m) => m.id == messageId);
    if (index != -1) {
      _messages[index] = _messages[index].copyWith(status: MessageStatus.delivered);
      notifyListeners();
    }
  }

  void _onMessageRead(String messageId, String userId) {
    // Update message status
    final index = _messages.indexWhere((m) => m.id == messageId);
    if (index != -1) {
      _messages[index] = _messages[index].copyWith(status: MessageStatus.read);
      notifyListeners();
    }
  }

  void _onReactionAdded(String messageId, String userId, String emoji) {
    // Update message reactions
    final index = _messages.indexWhere((m) => m.id == messageId);
    if (index != -1) {
      final reactions = Map<String, dynamic>.from(_messages[index].reactions ?? {});
      reactions[userId] = emoji;
      _messages[index] = _messages[index].copyWith(reactions: reactions);
      notifyListeners();
    }
  }

  void _onUserOnline(String userId) {
    if (!_onlineUsers.contains(userId)) {
      _onlineUsers.add(userId);
      notifyListeners();
    }
  }

  void _onUserOffline(String userId) {
    _onlineUsers.remove(userId);
    notifyListeners();
  }

  void _onOnlineUsers(List<String> users) {
    _onlineUsers = users;
    notifyListeners();
  }

  void _onError(String error) {
    _error = error;
    notifyListeners();
  }

  @override
  void dispose() {
    _chatService.disconnect();
    super.dispose();
  }
}
```

## 🎨 UI Components

### Chat Screen
```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ChatScreen extends StatefulWidget {
  final String recipientId;
  final String recipientName;

  const ChatScreen({
    Key? key,
    required this.recipientId,
    required this.recipientName,
  }) : super(key: key);

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  bool _isTyping = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ChatProvider>().getOnlineUsers();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.recipientName),
            Consumer<ChatProvider>(
              builder: (context, chatProvider, child) {
                final isOnline = chatProvider.onlineUsers.contains(widget.recipientId);
                return Text(
                  isOnline ? 'Online' : 'Offline',
                  style: const TextStyle(fontSize: 12),
                );
              },
            ),
          ],
        ),
        actions: [
          Consumer<ChatProvider>(
            builder: (context, chatProvider, child) {
              return IconButton(
                icon: Icon(
                  chatProvider.isConnected ? Icons.wifi : Icons.wifi_off,
                  color: chatProvider.isConnected ? Colors.green : Colors.red,
                ),
                onPressed: () {
                  if (!chatProvider.isConnected) {
                    // Reconnect logic
                  }
                },
              );
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: Consumer<ChatProvider>(
              builder: (context, chatProvider, child) {
                return ListView.builder(
                  controller: _scrollController,
                  itemCount: chatProvider.messages.length,
                  itemBuilder: (context, index) {
                    final message = chatProvider.messages[index];
                    return _buildMessageBubble(message);
                  },
                );
              },
            ),
          ),
          _buildTypingIndicator(),
          _buildMessageInput(),
        ],
      ),
    );
  }

  Widget _buildMessageBubble(ChatMessage message) {
    final isMe = message.senderId == context.read<ChatProvider>()._chatService._userId;
    
    return Align(
      alignment: isMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 16),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: isMe ? Colors.blue : Colors.grey[300],
          borderRadius: BorderRadius.circular(16),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              message.content,
              style: TextStyle(
                color: isMe ? Colors.white : Colors.black,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              _formatTimestamp(message.timestamp),
              style: TextStyle(
                fontSize: 12,
                color: isMe ? Colors.white70 : Colors.grey[600],
              ),
            ),
            if (message.reactions != null && message.reactions!.isNotEmpty)
              _buildReactions(message.reactions!),
          ],
        ),
      ),
    );
  }

  Widget _buildReactions(Map<String, dynamic> reactions) {
    return Wrap(
      spacing: 4,
      children: reactions.entries.map((entry) {
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: Colors.white.withOpacity(0.2),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            '${entry.value} ${entry.key}',
            style: const TextStyle(fontSize: 12),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildTypingIndicator() {
    return Consumer<ChatProvider>(
      builder: (context, chatProvider, child) {
        final isTyping = chatProvider.typingUsers[widget.recipientId] ?? false;
        if (!isTyping) return const SizedBox.shrink();

        return Container(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              const SizedBox(width: 16),
              Text(
                '${widget.recipientName} is typing...',
                style: TextStyle(
                  color: Colors.grey[600],
                  fontStyle: FontStyle.italic,
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildMessageInput() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.grey.withOpacity(0.2),
            spreadRadius: 1,
            blurRadius: 5,
            offset: const Offset(0, -1),
          ),
        ],
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _messageController,
              decoration: const InputDecoration(
                hintText: 'Type a message...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.all(Radius.circular(24)),
                ),
                contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              ),
              onChanged: (text) {
                if (text.isNotEmpty && !_isTyping) {
                  _isTyping = true;
                  context.read<ChatProvider>().sendTypingIndicator(
                    recipientId: widget.recipientId,
                    isTyping: true,
                  );
                } else if (text.isEmpty && _isTyping) {
                  _isTyping = false;
                  context.read<ChatProvider>().sendTypingIndicator(
                    recipientId: widget.recipientId,
                    isTyping: false,
                  );
                }
              },
              onSubmitted: (text) {
                if (text.trim().isNotEmpty) {
                  _sendMessage(text.trim());
                }
              },
            ),
          ),
          const SizedBox(width: 8),
          IconButton(
            icon: const Icon(Icons.send),
            onPressed: () {
              if (_messageController.text.trim().isNotEmpty) {
                _sendMessage(_messageController.text.trim());
              }
            },
          ),
        ],
      ),
    );
  }

  void _sendMessage(String content) {
    context.read<ChatProvider>().sendMessage(
      recipientId: widget.recipientId,
      content: content,
    );
    _messageController.clear();
    
    if (_isTyping) {
      _isTyping = false;
      context.read<ChatProvider>().sendTypingIndicator(
        recipientId: widget.recipientId,
        isTyping: false,
      );
    }
  }

  String _formatTimestamp(int timestamp) {
    final date = DateTime.fromMillisecondsSinceEpoch(timestamp * 1000);
    final now = DateTime.now();
    final difference = now.difference(date);

    if (difference.inDays > 0) {
      return '${date.day}/${date.month} ${date.hour}:${date.minute.toString().padLeft(2, '0')}';
    } else if (difference.inHours > 0) {
      return '${date.hour}:${date.minute.toString().padLeft(2, '0')}';
    } else {
      return '${date.minute.toString().padLeft(2, '0')}:${date.second.toString().padLeft(2, '0')}';
    }
  }
}
```

## 🚀 App Setup

### Main App
```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => ChatProvider()),
        // Add other providers as needed
      ],
      child: MaterialApp(
        title: 'Coptic Chat',
        theme: ThemeData(
          primarySwatch: Colors.blue,
          visualDensity: VisualDensity.adaptivePlatformDensity,
        ),
        home: const LoginScreen(),
      ),
    );
  }
}
```

### Login Screen
```dart
class LoginScreen extends StatefulWidget {
  const LoginScreen({Key? key}) : super(key: key);

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final TextEditingController _emailController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();
  bool _isLoading = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Login')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            TextField(
              controller: _emailController,
              decoration: const InputDecoration(labelText: 'Email'),
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _passwordController,
              decoration: const InputDecoration(labelText: 'Password'),
              obscureText: true,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _isLoading ? null : _login,
              child: _isLoading
                  ? const CircularProgressIndicator()
                  : const Text('Login'),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _login() async {
    setState(() => _isLoading = true);

    try {
      // Call your authentication API here
      final response = await _authenticate(
        _emailController.text,
        _passwordController.text,
      );

      if (response['success']) {
        final jwtToken = response['token'];
        
        // Initialize chat service
        await context.read<ChatProvider>().initialize(jwtToken);
        
        // Navigate to chat list
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (context) => const ChatListScreen(),
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(response['message'])),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Login failed: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<Map<String, dynamic>> _authenticate(String email, String password) async {
    // Implement your authentication logic here
    // This should call your backend API
    return {
      'success': true,
      'token': 'your-jwt-token-here',
    };
  }
}
```

## 🔧 Configuration

### Environment Setup
```dart
class AppConfig {
  static const String baseUrl = 'http://162.243.165.212:5000';
  static const String wsUrl = 'ws://162.243.165.212:5000/chatHub';
  
  // For development
  // static const String baseUrl = 'http://localhost:5199';
  // static const String wsUrl = 'ws://localhost:5199/chatHub';
}
```

## 📱 Usage Example

```dart
// Initialize chat service
final chatProvider = context.read<ChatProvider>();
await chatProvider.initialize(jwtToken);

// Send a message
await chatProvider.sendMessage(
  recipientId: 'user-id',
  content: 'Hello!',
);

// Send a media message
await chatProvider.sendMediaMessage(
  recipientId: 'user-id',
  fileUrl: 'https://example.com/image.jpg',
  fileName: 'image.jpg',
  fileSize: 1024,
  fileType: 'image/jpeg',
  messageType: MessageType.image,
);

// Send broadcast message
await chatProvider.sendBroadcastMessage(
  content: 'Hello everyone!',
);

// Listen for messages
chatProvider.addListener(() {
  final messages = chatProvider.messages;
  // Update UI with new messages
});
```

## 🚨 Error Handling

```dart
// Handle connection errors
chatProvider.addListener(() {
  if (chatProvider.error != null) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Error: ${chatProvider.error}'),
        action: SnackBarAction(
          label: 'Retry',
          onPressed: () {
            // Retry connection
            chatProvider.initialize(jwtToken);
          },
        ),
      ),
    );
  }
});
```

## 🔒 Security Notes

1. **JWT Token Storage**: Store JWT tokens securely using `flutter_secure_storage`
2. **Token Refresh**: Implement automatic token refresh before expiration
3. **Certificate Pinning**: Use certificate pinning for production
4. **Input Validation**: Validate all user inputs before sending
5. **Rate Limiting**: Implement client-side rate limiting for message sending

## 📚 Additional Resources

- [SignalR Flutter Client](https://pub.dev/packages/signalr_netcore_client)
- [Flutter WebSocket Guide](https://flutter.dev/docs/cookbook/networking/web-sockets)
- [Provider State Management](https://pub.dev/packages/provider)
- [Freezed Code Generation](https://pub.dev/packages/freezed)

This Flutter integration provides a complete real-time chat solution with your Coptic Chat Backend! 🚀
