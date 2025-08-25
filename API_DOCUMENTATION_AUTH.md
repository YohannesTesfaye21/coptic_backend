# Authentication & User Management API Documentation

## Overview
This section covers all authentication-related endpoints and user management operations. The system uses JWT tokens for authentication and supports two user types: **Abune** (community leaders) and **Regular** users.

## Base URL
```
https://localhost:7061/api
```

## Authentication Flow
1. **Register** → Get user account created (requires approval for Regular users)
2. **Login** → Get JWT token for API access
3. **Use JWT token** in Authorization header for protected endpoints
4. **Token expires** after 1 hour (3600 seconds)

---

## 🔐 Authentication Endpoints

### 1. User Registration

#### Regular User Registration
```http
POST /api/Auth/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "securePassword123",
  "name": "John Doe",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "deviceToken": "fcm-device-token-here"
}
```

**Success Response (201):**
```json
{
  "message": "Registration successful",
  "userId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "email": "john@example.com",
  "fullName": "John Doe",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "deviceToken": "fcm-device-token-here",
  "userType": "Regular",
  "isApproved": false,
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "requiresConfirmation": true
}
```

**Error Response (400):**
```json
{
  "error": "Validation failed",
  "message": "Email already exists"
}
```

#### Abune User Registration
```http
POST /api/Auth/register-abune
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "abune_michael",
  "email": "abune@church.com",
  "password": "securePassword123",
  "name": "Abune Michael",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "deviceToken": "fcm-device-token-here",
  "churchName": "St. Mary Coptic Church",
  "location": "New York, NY"
}
```

**Success Response (201):**
```json
{
  "message": "Abune registration successful",
  "userId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "email": "abune@church.com",
  "fullName": "Abune Michael",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "deviceToken": "fcm-device-token-here",
  "userType": "Abune",
  "isApproved": true,
  "churchName": "St. Mary Coptic Church",
  "location": "New York, NY",
  "requiresConfirmation": false
}
```

### 2. User Login

```http
POST /api/Auth/login
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "john@example.com",
  "password": "securePassword123",
  "deviceToken": "fcm-device-token-here"
}
```

**Success Response (200):**
```json
{
  "message": "Login successful",
  "userId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "email": "john@example.com",
  "fullName": "John Doe",
  "gender": "Male",
  "phoneNumber": "+1234567890",
  "deviceToken": "fcm-device-token-here",
  "userType": "Regular",
  "isApproved": true,
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "7cf9c456-0594-432a-9d5e-5f2420377f86",
  "expiresIn": 3600,
  "requiresConfirmation": false
}
```

**Error Responses:**

**Invalid Credentials (401):**
```json
{
  "error": "Authentication failed",
  "message": "Invalid email or password"
}
```

**Account Not Approved (403):**
```json
{
  "error": "Account pending approval",
  "message": "Your account is pending approval from your Abune"
}
```

**Account Not Active (403):**
```json
{
  "error": "Account not active",
  "message": "Your account is not active. Please contact your Abune."
}
```

### 3. Update Device Token

```http
POST /api/Auth/update-device-token
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "deviceToken": "new-fcm-device-token"
}
```

**Success Response (200):**
```json
{
  "message": "Device token updated successfully"
}
```

---

## 👥 User Management Endpoints

### 1. Get User Profile

```http
GET /api/User/{userId}
Authorization: Bearer {JWT_TOKEN}
```

**Success Response (200):**
```json
{
  "id": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "username": "john_doe",
  "email": "john@example.com",
  "phoneNumber": "+1234567890",
  "name": "John Doe",
  "gender": "Male",
  "deviceToken": "fcm-device-token-here",
  "userType": "Regular",
  "userStatus": "Active",
  "abuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "isApproved": true,
  "approvedAt": 1755976581,
  "approvedBy": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "createdAt": 1755976581,
  "lastModified": 1755976581
}
```

### 2. Update User Profile

```http
PUT /api/User/{userId}
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "John Smith",
  "phoneNumber": "+1987654321",
  "gender": "Male"
}
```

**Success Response (200):**
```json
{
  "message": "User updated successfully",
  "userId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310"
}
```

### 3. Get Community Members (Abune Only)

```http
GET /api/User/community-members
Authorization: Bearer {JWT_TOKEN}
```

**Success Response (200):**
```json
[
  {
    "id": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
    "username": "john_doe",
    "email": "john@example.com",
    "name": "John Doe",
    "gender": "Male",
    "userType": "Regular",
    "userStatus": "Active",
    "isApproved": true,
    "approvedAt": 1755976581,
    "createdAt": 1755976581
  }
]
```

**Error Response (403):**
```json
{
  "error": "Access denied",
  "message": "Only Abune users can access community members"
}
```

---

## 🔑 JWT Token Structure

The JWT token contains the following claims:

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "john@example.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "john",
  "UserType": "Regular",
  "UserId": "4ec2a7ad-2c91-4843-a5d4-69d7875d1310",
  "AbuneId": "7ddcc57a-bead-4169-b141-4ad9ae246805",
  "exp": 1755976581,
  "iss": "coptic-app-backend",
  "aud": "coptic-app-frontend"
}
```

---

## 📱 Frontend Implementation Notes

### 1. Token Storage
- Store JWT token in secure storage (e.g., `localStorage` or `sessionStorage`)
- Include token in all API requests: `Authorization: Bearer {token}`

### 2. Token Refresh
- Monitor token expiration (3600 seconds)
- Implement automatic logout when token expires
- Consider implementing refresh token logic

### 3. Error Handling
- Handle 401 responses by redirecting to login
- Handle 403 responses by showing appropriate error messages
- Implement retry logic for network errors

### 4. User Approval Flow
- Show approval status to Regular users
- Display appropriate UI based on `isApproved` status
- Guide users through approval process

### 5. Device Token Management
- Update device token on app startup
- Handle device token changes
- Ensure notifications work properly
