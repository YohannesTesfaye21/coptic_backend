# API Quick Reference Guide

## 🚀 **Quick Start Checklist**

### **1. Environment Setup**
- [ ] Backend running on `https://localhost:7061`
- [ ] PostgreSQL database accessible
- [ ] File storage directories created
- [ ] JWT secret key configured

### **2. First API Call**
```bash
# Test if API is running
curl https://localhost:7061/api/health

# Expected: 200 OK
```

### **3. User Registration Flow**
```bash
# 1. Register Regular User
curl -X POST https://localhost:7061/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "test_user",
    "email": "test@example.com",
    "password": "password123",
    "name": "Test User",
    "phoneNumber": "+1234567890",
    "deviceToken": "fcm-token-here"
  }'

# 2. Login to get JWT token
curl -X POST https://localhost:7061/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "password123",
    "deviceToken": "fcm-token-here"
  }'
```

---

## 📋 **API Endpoints Summary**

### **🔐 Authentication**
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/Auth/register` | ❌ | Register Regular User |
| `POST` | `/api/Auth/register-abune` | ❌ | Register Abune User |
| `POST` | `/api/Auth/login` | ❌ | User Login |
| `POST` | `/api/Auth/update-device-token` | ✅ | Update Device Token |

### **👥 User Management**
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/User/{userId}` | ✅ | Get User Profile |
| `PUT` | `/api/User/{userId}` | ✅ | Update User Profile |
| `GET` | `/api/User/community-members` | ✅ | Get Community Members (Abune Only) |

### **💬 Chat System**
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/Chat/send` | ✅ | Send Text Message |
| `POST` | `/api/Chat/send-media` | ✅ | Send Media Message |
| `POST` | `/api/Chat/broadcast` | ✅ | Send Broadcast (Abune Only) |
| `GET` | `/api/Chat/conversation/{otherUserId}` | ✅ | Get Conversation Messages |
| `GET` | `/api/Chat/conversations` | ✅ | Get User Conversations |
| `POST` | `/api/Chat/mark-read/{messageId}` | ✅ | Mark Message as Read |
| `GET` | `/api/Chat/unread-count` | ✅ | Get Unread Count |
| `GET` | `/api/Chat/search` | ✅ | Search Messages |

### **📁 File Management**
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/FileUpload/chat-file` | ✅ | Upload Chat File |
| `POST` | `/api/FileUpload/broadcast-file` | ✅ | Upload Broadcast File (Abune Only) |
| `GET` | `/api/FileUpload/chat-files` | ✅ | Get Chat Files |
| `GET` | `/api/FileUpload/broadcast-files` | ✅ | Get Broadcast Files |
| `GET` | `/api/FileUpload/file-info` | ✅ | Get File Info |
| `DELETE` | `/api/FileUpload/delete` | ✅ | Delete File |

---

## 🔑 **Authentication Headers**

### **JWT Token Format**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### **Content Type Headers**
```http
Content-Type: application/json
Content-Type: multipart/form-data  # For file uploads
```

---

## 📊 **Data Models Quick Reference**

### **User Types**
```json
{
  "UserType": {
    "Regular": 0,
    "Abune": 1
  }
}
```

### **User Status**
```json
{
  "UserStatus": {
    "Active": 0,
    "Inactive": 1,
    "Suspended": 2,
    "PendingApproval": 3
  }
}
```

### **Message Types**
```json
{
  "MessageType": {
    "Text": 0,
    "Image": 1,
    "Document": 2,
    "Voice": 3
  }
}
```

### **Message Status**
```json
{
  "MessageStatus": {
    "Sent": 0,
    "Delivered": 1,
    "Read": 2,
    "Failed": 3
  }
}
```

---

## 🚨 **Common HTTP Status Codes**

| Code | Meaning | Action Required |
|------|---------|-----------------|
| **200** | Success | Continue with response data |
| **201** | Created | Resource created successfully |
| **400** | Bad Request | Check request format/validation |
| **401** | Unauthorized | Provide valid JWT token |
| **403** | Forbidden | User lacks permission |
| **404** | Not Found | Resource doesn't exist |
| **500** | Server Error | Contact backend team |

---

## 📱 **SignalR Hub Quick Reference**

### **Connection**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7061/chatHub")
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### **Key Methods**
```javascript
// Join community
await connection.invoke("JoinCommunity", abuneId);

// Send message
await connection.invoke("SendMessage", messageData);

// Start typing
await connection.invoke("StartTyping", typingData);

// Stop typing
await connection.invoke("StopTyping", typingData);
```

### **Key Events**
```javascript
// Listen for messages
connection.on("ReceiveMessage", handleMessage);

// Listen for typing
connection.on("UserTyping", handleTyping);

// Listen for status changes
connection.on("UserStatusChanged", handleStatusChange);
```

---

## 🧪 **Testing Examples**

### **1. Test User Registration**
```bash
curl -X POST https://localhost:7061/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_doe",
    "email": "john@example.com",
    "password": "password123",
    "name": "John Doe",
    "gender": "Male",
    "phoneNumber": "+1234567890",
    "deviceToken": "fcm-token-here"
  }'
```

### **2. Test User Login**
```bash
curl -X POST https://localhost:7061/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "password123",
    "deviceToken": "fcm-token-here"
  }'
```

### **3. Test Send Message (with JWT)**
```bash
curl -X POST https://localhost:7061/api/Chat/send \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "abune-user-id",
    "content": "Hello, this is a test message",
    "messageType": 0
  }'
```

### **4. Test File Upload**
```bash
curl -X POST https://localhost:7061/api/FileUpload/chat-file \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/image.jpg" \
  -F "recipientId=abune-user-id" \
  -F "messageType=1"
```

---

## 📋 **Postman Collection**

### **Import this JSON into Postman:**

```json
{
  "info": {
    "name": "Coptic App Backend API",
    "description": "Complete API collection for Coptic App Backend",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:7061/api",
      "type": "string"
    },
    {
      "key": "jwtToken",
      "value": "",
      "type": "string"
    },
    {
      "key": "userId",
      "value": "",
      "type": "string"
    },
    {
      "key": "abuneId",
      "value": "",
      "type": "string"
    }
  ],
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{jwtToken}}",
        "type": "string"
      }
    ]
  },
  "item": [
    {
      "name": "Authentication",
      "item": [
        {
          "name": "Register Regular User",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"username\": \"test_user\",\n  \"email\": \"test@example.com\",\n  \"password\": \"password123\",\n  \"name\": \"Test User\",\n  \"gender\": \"Male\",\n  \"phoneNumber\": \"+1234567890\",\n  \"deviceToken\": \"fcm-token-here\"\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/Auth/register",
              "host": ["{{baseUrl}}"],
              "path": ["Auth", "register"]
            }
          }
        },
        {
          "name": "Register Abune User",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"username\": \"abune_test\",\n  \"email\": \"abune@example.com\",\n  \"password\": \"password123\",\n  \"name\": \"Test Abune\",\n  \"gender\": \"Male\",\n  \"phoneNumber\": \"+1234567890\",\n  \"deviceToken\": \"fcm-token-here\",\n  \"churchName\": \"Test Church\",\n  \"location\": \"Test City\"\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/Auth/register-abune",
              "host": ["{{baseUrl}}"],
              "path": ["Auth", "register-abune"]
            }
          }
        },
        {
          "name": "Login",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"email\": \"test@example.com\",\n  \"password\": \"password123\",\n  \"deviceToken\": \"fcm-token-here\"\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/Auth/login",
              "host": ["{{baseUrl}}"],
              "path": ["Auth", "login"]
            }
          },
          "event": [
            {
              "listen": "test",
              "script": {
                "exec": [
                  "if (pm.response.code === 200) {",
                  "    const response = pm.response.json();",
                  "    pm.collectionVariables.set('jwtToken', response.accessToken);",
                  "    pm.collectionVariables.set('userId', response.userId);",
                  "    pm.collectionVariables.set('abuneId', response.abuneId);",
                  "}"
                ]
              }
            }
          ]
        }
      ]
    },
    {
      "name": "Chat",
      "item": [
        {
          "name": "Send Message",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"recipientId\": \"{{abuneId}}\",\n  \"content\": \"Hello, this is a test message\",\n  \"messageType\": 0\n}"
            },
            "url": {
              "raw": "{{baseUrl}}/Chat/send",
              "host": ["{{baseUrl}}"],
              "path": ["Chat", "send"]
            }
          }
        },
        {
          "name": "Get Conversations",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{baseUrl}}/Chat/conversations",
              "host": ["{{baseUrl}}"],
              "path": ["Chat", "conversations"]
            }
          }
        },
        {
          "name": "Get Conversation Messages",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{baseUrl}}/Chat/conversation/{{abuneId}}?limit=50",
              "host": ["{{baseUrl}}"],
              "path": ["Chat", "conversation", "{{abuneId}}"],
              "query": [
                {
                  "key": "limit",
                  "value": "50"
                }
              ]
            }
          }
        }
      ]
    },
    {
      "name": "File Upload",
      "item": [
        {
          "name": "Upload Chat File",
          "request": {
            "method": "POST",
            "header": [],
            "body": {
              "mode": "formdata",
              "formdata": [
                {
                  "key": "file",
                  "type": "file",
                  "src": []
                },
                {
                  "key": "recipientId",
                  "value": "{{abuneId}}",
                  "type": "text"
                },
                {
                  "key": "messageType",
                  "value": "1",
                  "type": "text"
                }
              ]
            },
            "url": {
              "raw": "{{baseUrl}}/FileUpload/chat-file",
              "host": ["{{baseUrl}}"],
              "path": ["FileUpload", "chat-file"]
            }
          }
        }
      ]
    },
    {
      "name": "User Management",
      "item": [
        {
          "name": "Get User Profile",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{baseUrl}}/User/{{userId}}",
              "host": ["{{baseUrl}}"],
              "path": ["User", "{{userId}}"]
            }
          }
        },
        {
          "name": "Get Community Members",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{baseUrl}}/User/community-members",
              "host": ["{{baseUrl}}"],
              "path": ["User", "community-members"]
            }
          }
        }
      ]
    }
  ]
}
```

---

## 🔧 **Troubleshooting**

### **Common Issues & Solutions**

#### **1. 401 Unauthorized**
- **Problem**: JWT token missing or expired
- **Solution**: Re-login to get fresh token

#### **2. 403 Forbidden**
- **Problem**: User lacks permission for action
- **Solution**: Check user type and community membership

#### **3. 500 Internal Server Error**
- **Problem**: Backend server error
- **Solution**: Check server logs, contact backend team

#### **4. Connection Refused**
- **Problem**: Backend not running
- **Solution**: Start backend server on port 7061

#### **5. File Upload Fails**
- **Problem**: File too large or invalid type
- **Solution**: Check file size limits and allowed types

---

## 📞 **Support & Resources**

### **Documentation Files**
- `API_DOCUMENTATION_OVERVIEW.md` - Complete system overview
- `API_DOCUMENTATION_AUTH.md` - Authentication details
- `API_DOCUMENTATION_CHAT.md` - Chat system details
- `API_DOCUMENTATION_FILES.md` - File management details
- `API_DOCUMENTATION_WEBSOCKET.md` - WebSocket details

### **Development Tools**
- **Swagger UI**: `https://localhost:7061/swagger`
- **Database**: PostgreSQL with Entity Framework
- **Real-time**: SignalR WebSocket hub
- **File Storage**: Local file system with structured directories

### **Testing Tools**
- **Postman**: Import collection above
- **cURL**: Command-line examples provided
- **Swagger**: Interactive API testing
- **SignalR Client**: Test real-time features
