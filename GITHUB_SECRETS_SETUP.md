# GitHub Secrets Setup Guide

This guide explains how to set up GitHub secrets for the Coptic App Backend to securely store sensitive configuration data.

## Required GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions → New repository secret

### AWS Configuration
- **Name**: `AWS_ACCESS_KEY_ID`
- **Value**: `YOUR_AWS_ACCESS_KEY_ID_HERE`

- **Name**: `AWS_SECRET_ACCESS_KEY`
- **Value**: `YOUR_AWS_SECRET_ACCESS_KEY_HERE`

- **Name**: `AWS_REGION`
- **Value**: `eu-north-1`

### Cognito Configuration
- **Name**: `COGNITO_USER_POOL_ID`
- **Value**: `YOUR_COGNITO_USER_POOL_ID_HERE`

- **Name**: `COGNITO_CLIENT_ID`
- **Value**: `YOUR_COGNITO_CLIENT_ID_HERE`

- **Name**: `COGNITO_CLIENT_SECRET`
- **Value**: (leave empty if not used)

### Database Configuration
- **Name**: `CONNECTION_STRING`
- **Value**: `Host=162.243.165.212;Port=5432;Database=coptic_app;Username=coptic_user;Password=your_secure_password_here;SSL Mode=Prefer;Trust Server Certificate=true`

### JWT Configuration
- **Name**: `JWT_KEY`
- **Value**: `this-is-a-very-long-secret-key-for-jwt-signing-that-is-at-least-64-characters-long-for-security`

## Local Development Setup

For local development, you can either:

### Option 1: Set Environment Variables
```bash
export AWS_ACCESS_KEY_ID="YOUR_AWS_ACCESS_KEY_ID_HERE"
export AWS_SECRET_ACCESS_KEY="YOUR_AWS_SECRET_ACCESS_KEY_HERE"
export AWS_REGION="eu-north-1"
export COGNITO_USER_POOL_ID="YOUR_COGNITO_USER_POOL_ID_HERE"
export COGNITO_CLIENT_ID="YOUR_COGNITO_CLIENT_ID_HERE"
```

### Option 2: Use appsettings.Development.json
Add the credentials back to `appsettings.Development.json` for local development only (this file should be in .gitignore).

## How It Works

The application now reads configuration in this priority order:
1. Environment variables (highest priority)
2. Configuration files (fallback)

This allows you to:
- Use environment variables in production (from GitHub secrets)
- Use configuration files for local development
- Keep sensitive data out of the codebase

## Security Benefits

- ✅ No hardcoded credentials in the codebase
- ✅ Credentials are encrypted in GitHub secrets
- ✅ Different credentials for different environments
- ✅ Easy to rotate credentials without code changes