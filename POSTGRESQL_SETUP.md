# PostgreSQL Setup Guide

This guide will help you set up PostgreSQL for the Coptic App Backend after removing AWS dependencies.

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL installed on your system
- A code editor (Visual Studio, VS Code, Rider, etc.)

## Step 1: Install PostgreSQL

### On macOS (using Homebrew):
```bash
brew install postgresql
brew services start postgresql
```

### On Windows:
Download and install from: https://www.postgresql.org/download/windows/

### On Ubuntu/Debian:
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

## Step 2: Configure PostgreSQL Port (Optional)

You might want to use a different port than the default 5432 for several reasons:
- **Security**: Running on a non-standard port can help avoid automated attacks
- **Multiple PostgreSQL instances**: If you have multiple PostgreSQL installations
- **Port conflicts**: If port 5432 is already in use by another service
- **Development environments**: To avoid conflicts with other projects

Common alternative ports: 5433, 5434, 54321, etc.

If you want to use a different port than the default 5432:

### On macOS/Linux:
1. Find your PostgreSQL configuration file:
```bash
# Usually located at one of these paths:
# /usr/local/var/postgres/postgresql.conf (Homebrew on macOS)
# /etc/postgresql/[version]/main/postgresql.conf (Ubuntu/Debian)
# /var/lib/pgsql/data/postgresql.conf (CentOS/RHEL)
```

2. Edit the configuration file:
```bash
sudo nano /usr/local/var/postgres/postgresql.conf
```

3. Find and modify the port setting:
```
# Uncomment and change the port number
port = 5433  # Change from default 5432 to your desired port
```

4. Restart PostgreSQL:
```bash
# macOS (Homebrew)
brew services restart postgresql

# Ubuntu/Debian
sudo systemctl restart postgresql

# CentOS/RHEL
sudo systemctl restart postgresql
```

### On Windows:
1. Open PostgreSQL configuration file (usually in `C:\Program Files\PostgreSQL\[version]\data\postgresql.conf`)
2. Find and modify: `port = 5433`
3. Restart PostgreSQL service through Services panel or:
```cmd
net stop postgresql-x64-[version]
net start postgresql-x64-[version]
```

## Step 3: Create Database and User

1. Connect to PostgreSQL as the postgres user (using custom port if changed):
```bash
# Default port
sudo -u postgres psql

# Custom port (replace 5433 with your chosen port)
sudo -u postgres psql -p 5433
```

2. Create a new database and user:
```sql
CREATE DATABASE coptic_app;
CREATE USER coptic_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE coptic_app TO coptic_user;
\q
```

## Step 4: Update Configuration

1. Open `coptic_app_backend.Api/appsettings.json`
2. Update the connection string with your database details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=coptic_app;Username=coptic_user;Password=your_secure_password"
  }
}
```

**Note:** If you're using the default port (5432), you can omit the `Port` parameter:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=coptic_app;Username=coptic_user;Password=your_secure_password"
  }
}
```

## Step 5: Run Database Migrations

1. Navigate to the API project directory:
```bash
cd coptic_app_backend.Api
```

2. Run the Entity Framework migrations:
```bash
dotnet ef database update
```

This will create all the necessary tables in your PostgreSQL database.

## Step 6: Build and Run the Application

1. Build the solution:
```bash
dotnet build
```

2. Run the API:
```bash
dotnet run
```

The application should now be running with PostgreSQL as the database.

## Database Schema

The application will create the following tables:

### Users Table
- `Id` (Primary Key)
- `Username` (Unique)
- `Email` (Unique)
- `Name`
- `GivenName`
- `FamilyName`
- `PhoneNumber`
- `Gender`
- `DeviceToken`
- `PasswordHash`
- `EmailVerified`
- `PhoneNumberVerified`
- `Status`
- `CreatedAt`
- `LastModified`

### ChatMessages Table
- `Id` (Primary Key)
- `SenderId`
- `SenderName`
- `SenderEmail`
- `Content`
- `MessageType`
- `TargetUserId`
- `RecipientId`
- `Timestamp`
- `IsRead`
- `ReadBy` (JSON array)
- `Status`
- `MessageContent`
- `IsDeleted`
- `DeletedAt`
- `DeletedBy`
- `IsEdited`
- `EditedAt`
- `OriginalContent`
- `Reactions` (JSON array)
- `FileUrl`
- `FileType`
- `FileSize`
- `FileName`
- `ReplyToMessageId`
- `ForwardedFromMessageId`

## Features

### Authentication
- Local user registration and login
- JWT token-based authentication
- Password hashing using SHA256
- User management

### Chat System
- Real-time messaging using SignalR
- Message persistence in PostgreSQL
- File uploads (stored locally)
- Message reactions
- User blocking (placeholder implementation)

### File Storage
- Local file storage in `wwwroot/uploads`
- Support for images, documents, audio files
- File type validation
- Secure file access

## Troubleshooting

### Common Issues

1. **Connection String Errors**
   - Verify PostgreSQL is running on the correct port
   - Check username, password, database name, and port number
   - Ensure the user has proper permissions
   - Test connection: `psql -h localhost -p [PORT] -U [USERNAME] -d [DATABASE]`

2. **Migration Errors**
   - Make sure you're in the correct directory (`coptic_app_backend.Api`)
   - Verify the connection string is correct
   - Check that PostgreSQL is accessible

3. **Build Errors**
   - Ensure all NuGet packages are restored
   - Check that .NET 8.0 SDK is installed
   - Verify all project references are correct

### Useful Commands

```bash
# Check PostgreSQL status
brew services list | grep postgresql

# Check which port PostgreSQL is running on
sudo netstat -tlnp | grep postgres
# or on macOS:
lsof -i :5432
lsof -i :5433

# Test PostgreSQL connection
pg_isready -h localhost -p 5433

# Connect to database (default port)
psql -h localhost -U coptic_user -d coptic_app

# Connect to database (custom port)
psql -h localhost -p 5433 -U coptic_user -d coptic_app

# List tables
\dt

# View table structure
\d+ users
\d+ chat_messages

# Check current PostgreSQL configuration
SHOW port;
SHOW config_file;

# Reset database (if needed)
dotnet ef database drop
dotnet ef database update
```

## Security Notes

- Change default passwords
- Use strong, unique passwords
- Consider using environment variables for sensitive configuration
- Implement proper input validation
- Use HTTPS in production

## Next Steps

1. Test the API endpoints using Swagger UI
2. Implement proper email confirmation
3. Add password reset functionality
4. Implement user blocking system
5. Add file upload size limits
6. Implement proper JWT token validation
7. Add rate limiting
8. Set up logging and monitoring

## Support

If you encounter issues:
1. Check the application logs
2. Verify database connectivity
3. Ensure all dependencies are properly installed
4. Check the Entity Framework migration status
