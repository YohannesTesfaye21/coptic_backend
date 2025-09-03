# Coptic App Backend

This is the backend API for the Coptic App, built using .NET 8 with Clean Architecture principles.

## Project Structure

The solution follows Clean Architecture with the following projects:

- **coptic_app_backend.Domain**: Contains domain models, entities, and interfaces
- **coptic_app_backend.Application**: Contains application services and business logic
- **coptic_app_backend.Infrastructure**: Contains external service implementations (AWS, FCM)
- **coptic_app_backend.Api**: Contains the REST API controllers and configuration

## Prerequisites

- .NET 8 SDK
- AWS CLI configured with appropriate credentials
- Firebase Cloud Messaging project setup

## Configuration

1. Update `coptic_app_backend.Api/appsettings.json` with your configuration:
   - AWS Region and Profile
   - FCM Project ID and Service Account JSON
   - Cognito User Pool ID

2. Ensure your AWS credentials are configured with permissions for:
   - DynamoDB (create, read, update, delete tables and items)
   - Cognito Identity Provider

## Running the Application

1. Navigate to the solution directory:
   ```bash
   cd /Users/m2pro/RiderProjects/coptic_app_backend
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the API project:
   ```bash
   dotnet run --project coptic_app_backend.Api
   ```

The API will be available at `https://localhost:5001` (or the configured port).

## API Endpoints

### Chat
- `GET /api/chat/messages?userId={userId}&limit={limit}` - Get messages for a user
- `POST /api/chat/messages` - Post a new message
- `PUT /api/chat/messages/{messageId}` - Update a message
- `GET /api/chat/messages/user/{userId}?limit={limit}` - Get messages sent to a specific user

### Users
- `GET /api/user` - Get all users
- `POST /api/user` - Create a new user
- `PUT /api/user/{userId}` - Update a user
- `POST /api/user/{userId}/device-token` - Register device token for notifications
- `POST /api/user/{userId}/test-notification` - Send test notification

## Database Schema

The application uses DynamoDB with the following tables:

### ChatMessages
- Primary Key: `id` (String)
- GSI: `senderId` (String)
- GSI: `targetUserId` (String)
- Attributes: senderName, content, messageType, timestamp, isRead, readBy

### Users
- Primary Key: `id` (String)
- Attributes: username, email, deviceToken, createdAt, lastSeen

## Architecture Notes

- **Domain Layer**: Contains pure business logic and interfaces
- **Application Layer**: Orchestrates domain services and implements business rules
- **Infrastructure Layer**: Implements external service integrations
- **API Layer**: Handles HTTP requests and responses

The application uses dependency injection to maintain loose coupling between layers.
# Test deployment with GitHub Secrets
