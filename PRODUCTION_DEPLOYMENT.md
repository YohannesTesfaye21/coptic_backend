# 🚀 Production Server Deployment Guide

## Server Access
```bash
ssh -i ~/.ssh/id_ed25519 root@162.243.165.212
```

## 🔐 Setting Up AWS Credentials on Production Server

### Step 1: SSH into your server
```bash
ssh -i ~/.ssh/id_ed25519 root@162.243.165.212
```

### Step 2: Create environment file for your application
```bash
# Create a secure environment file for your .NET application
sudo nano /etc/environment
```

### Step 3: Add your AWS credentials to the system environment
Add these lines to `/etc/environment`:
```bash
AWS_ACCESS_KEY_ID="your_aws_access_key_here"
AWS_SECRET_ACCESS_KEY="your_aws_secret_key_here"
AWS_REGION="eu-north-1"
COGNITO_USER_POOL_ID="eu-north-1_Kssmj7nVD"
COGNITO_CLIENT_ID="dd1g2nhp09bdcklaq2ab1du1l"
CONNECTION_STRING="Host=162.243.165.212;Port=5432;Database=coptic_app;Username=coptic_user;Password=your_secure_password_here;SSL Mode=Prefer;Trust Server Certificate=true"
JWT_KEY="this-is-a-very-long-secret-key-for-jwt-signing-that-is-at-least-64-characters-long-for-security"
```

### Step 4: Alternative - Create a .env file for your application
```bash
# Navigate to your application directory
cd /path/to/your/coptic_app_backend

# Create production environment file
sudo nano production.env
```

Add the same variables to `production.env`:
```bash
AWS_ACCESS_KEY_ID=your_aws_access_key_here
AWS_SECRET_ACCESS_KEY=your_aws_secret_key_here
AWS_REGION=eu-north-1
COGNITO_USER_POOL_ID=eu-north-1_Kssmj7nVD
COGNITO_CLIENT_ID=dd1g2nhp09bdcklaq2ab1du1l
CONNECTION_STRING=Host=162.243.165.212;Port=5432;Database=coptic_app;Username=coptic_user;Password=your_secure_password_here;SSL Mode=Prefer;Trust Server Certificate=true
JWT_KEY=this-is-a-very-long-secret-key-for-jwt-signing-that-is-at-least-64-characters-long-for-security
```

### Step 5: Set file permissions (Important for security)
```bash
# Make the environment file readable only by root
sudo chmod 600 /etc/environment
# OR if using production.env file:
sudo chmod 600 production.env
```

### Step 6: Apply environment variables immediately
```bash
# Reload environment variables
source /etc/environment
```

### Step 7: Verify environment variables are set
```bash
echo $AWS_ACCESS_KEY_ID
echo $AWS_REGION
echo $COGNITO_USER_POOL_ID
```

## 🔄 Running Your Application with Environment Variables

### Option A: Using systemd service (Recommended)
Create a systemd service file:
```bash
sudo nano /etc/systemd/system/coptic-app.service
```

Content:
```ini
[Unit]
Description=Coptic App Backend
After=network.target

[Service]
Type=notify
User=root
WorkingDirectory=/path/to/your/coptic_app_backend/coptic_app_backend.Api
ExecStart=/usr/bin/dotnet coptic_app_backend.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=coptic-app
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/etc/environment

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable coptic-app.service
sudo systemctl start coptic-app.service
sudo systemctl status coptic-app.service
```

### Option B: Direct dotnet run with environment
```bash
# Load environment and run
source /etc/environment && dotnet run --project coptic_app_backend.Api
```

### Option C: Using Docker (if you prefer containerization)
```bash
# Run with environment file
docker run -d --env-file production.env your-app-image
```

## 🔍 Troubleshooting

### Check if environment variables are loaded:
```bash
# Check specific variables
printenv | grep AWS
printenv | grep COGNITO
```

### Check application logs:
```bash
# If using systemd
sudo journalctl -u coptic-app.service -f

# Or check application directory logs
tail -f /path/to/your/app/logs/*.log
```

### Test environment loading:
```bash
# Create a simple test script
echo 'Console.WriteLine($"AWS Key: {Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")}");' > test.cs
dotnet script test.cs
```

## 🔒 Security Best Practices

1. **Never expose credentials in URLs or logs**
2. **Use restrictive file permissions (600)**
3. **Consider using AWS IAM roles instead of access keys**
4. **Regularly rotate credentials**
5. **Monitor AWS CloudTrail for unauthorized access**

## 📋 Quick Commands Summary

```bash
# SSH to server
ssh -i ~/.ssh/id_ed25519 root@162.243.165.212

# Edit environment
sudo nano /etc/environment

# Reload environment
source /etc/environment

# Check variables
printenv | grep AWS

# Restart service
sudo systemctl restart coptic-app.service
```
