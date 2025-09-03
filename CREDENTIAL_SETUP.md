# 🔐 Credential Setup Guide

## Overview
This guide explains how to properly configure AWS Cognito and database credentials for local development and production environments.

## 🚨 Security Warning
**NEVER commit real credentials to git repositories!** This can lead to security breaches and unauthorized access to your AWS resources.

## 📝 Local Development Setup

### Step 1: Create Local Environment File
1. Copy the example environment file:
   ```bash
   cp local.env.example local.env
   ```

2. Open `local.env` and replace all placeholder values with your actual credentials:
   ```bash
   # AWS Configuration
   AWS_ACCESS_KEY_ID=your_aws_access_key_here
   AWS_SECRET_ACCESS_KEY=your_actual_secret_key
   AWS_REGION=eu-north-1
   
   # Cognito Configuration
   COGNITO_USER_POOL_ID=eu-north-1_Kssmj7nVD
   COGNITO_CLIENT_ID=dd1g2nhp09bdcklaq2ab1du1l
   
   # Database Configuration
   CONNECTION_STRING=Host=your_host;Port=5432;Database=coptic_app;Username=coptic_user;Password=your_password;SSL Mode=Prefer;Trust Server Certificate=true
   
   # JWT Configuration
   JWT_KEY=your_very_long_secret_key_for_jwt_signing_that_is_at_least_64_characters_long_for_security
   ```

### Step 2: Verify Environment Loading
The application automatically loads `local.env` for development. Check the startup logs:
```
Loaded environment variables from: /path/to/local.env
```

### Step 3: Test the Application
```bash
dotnet run --project coptic_app_backend.Api
```

## 🚀 Production Deployment

### For Docker/Container Environments:
Set environment variables in your container orchestration platform:
```yaml
# docker-compose.yml or Kubernetes
environment:
  - AWS_ACCESS_KEY_ID=your_key
  - AWS_SECRET_ACCESS_KEY=your_secret
  - AWS_REGION=eu-north-1
  - COGNITO_USER_POOL_ID=your_pool_id
  - COGNITO_CLIENT_ID=your_client_id
  - CONNECTION_STRING=your_connection_string
  - JWT_KEY=your_jwt_key
```

### For GitHub Actions:
Use GitHub Secrets as described in `GITHUB_SECRETS_SETUP.md`

### For Traditional Servers:
Set system environment variables:
```bash
export AWS_ACCESS_KEY_ID="your_key"
export AWS_SECRET_ACCESS_KEY="your_secret"
export AWS_REGION="eu-north-1"
# ... other variables
```

## 🔧 Configuration Priority
The application loads credentials in this order:
1. **Environment Variables** (highest priority)
2. **local.env file** (development only)
3. **appsettings.json** (fallback, usually empty for security)

## 🛡️ Security Best Practices

### ✅ DO:
- Use `local.env` for local development
- Set environment variables in production
- Use AWS IAM roles when possible
- Rotate credentials regularly
- Use different credentials for different environments

### ❌ DON'T:
- Commit `local.env` to git
- Share credentials in chat/email
- Use production credentials in development
- Store credentials in source code
- Use overly permissive AWS policies

## 🐛 Troubleshooting

### Error: "AWS credentials are not configured properly"
1. Check if `local.env` exists in the project root
2. Verify all required variables are set
3. Restart the application
4. Check application startup logs

### Environment Variables Not Loading
1. Ensure `local.env` is in the same directory as the `.csproj` file
2. Check for typos in variable names
3. Restart your IDE/terminal

### Git Push Rejected (Secret Detection)
If you accidentally committed secrets:
1. Remove the commit: `git reset --hard HEAD~1`
2. Add credentials to `.gitignore` 
3. Recreate the `local.env` file (not committed)
4. Push again

## 📞 Support
If you encounter issues with credential setup, check:
1. This documentation
2. `GITHUB_SECRETS_SETUP.md` for CI/CD
3. AWS Cognito console for correct pool/client IDs
