# Firebase Credentials Setup for GitHub Actions CI/CD

This guide explains how to configure Firebase credentials for your server deployment using GitHub Actions CI/CD.

## Overview

Your server needs three types of Firebase credentials:
1. **FCM Service Account JSON** - For server-side push notifications
2. **google-services.json** - For Android apps (optional for server)
3. **GoogleService-Info.plist** - For iOS apps (optional for server)

## Required GitHub Secrets

Navigate to your GitHub repository → Settings → Secrets and variables → Actions, and add the following secrets:

### 1. FCM_PROJECT_ID
- **Description**: Your Firebase project ID
- **Value**: Your Firebase project ID (e.g., `coptic-6a5a1`)
- **Where to find**: Firebase Console → Project Settings → General → Project ID

### 2. FCM_SERVICE_ACCOUNT_JSON
- **Description**: Firebase service account JSON for server-side FCM
- **Value**: Complete JSON content of your service account file

#### How to get FCM Service Account JSON:
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Go to Project Settings (⚙️ icon) → Service accounts tab
4. Click "Generate new private key"
5. Download the JSON file
6. Copy the **entire content** of the JSON file and paste it as the secret value

**Example format:**
```json
{
  "type": "service_account",
  "project_id": "coptic-6a5a1",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk-xxxxx@coptic-6a5a1.iam.gserviceaccount.com",
  "client_id": "...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "..."
}
```

### 3. GOOGLE_SERVICES_JSON (Optional - for Android client reference)
- **Description**: Android app configuration file
- **Value**: Complete JSON content of google-services.json

#### How to get google-services.json:
1. Go to Firebase Console → Project Settings → General
2. Scroll down to "Your apps" section
3. Find your Android app or add a new one
4. Download `google-services.json`
5. Copy the entire content and paste as secret value

### 4. GOOGLE_SERVICE_INFO_PLIST (Optional - for iOS client reference)
- **Description**: iOS app configuration file
- **Value**: Complete XML content of GoogleService-Info.plist

#### How to get GoogleService-Info.plist:
1. Go to Firebase Console → Project Settings → General
2. Scroll down to "Your apps" section
3. Find your iOS app or add a new one
4. Download `GoogleService-Info.plist`
5. Copy the entire content and paste as secret value

## Deployment Process

When you push to the `main` branch, GitHub Actions will:

1. **Create Firebase directory** on your server: `~/coptic-app-backend/firebase/`
2. **Generate credential files** from secrets:
   - `service-account.json` → For FCM notifications
   - `google-services.json` → Available for client apps
   - `GoogleService-Info.plist` → Available for client apps
3. **Mount credentials** into Docker container at `/app/firebase/`
4. **Configure environment** variables for your .NET app

## File Locations

After deployment, Firebase credentials will be available at:

### On Server (Host)
- `~/coptic-app-backend/firebase/service-account.json`
- `~/coptic-app-backend/firebase/google-services.json`
- `~/coptic-app-backend/firebase/GoogleService-Info.plist`

### In Docker Container
- `/app/firebase/service-account.json` ← Used by your .NET app
- `/app/firebase/google-services.json`
- `/app/firebase/GoogleService-Info.plist`

## Environment Variables

Your .NET application will have access to:
- `FCM__ProjectId` → Your Firebase project ID
- `FCM__ServiceAccountJson` → `/app/firebase/service-account.json`

## Security Best Practices

1. **Never commit credential files** to your repository
2. **Use GitHub Secrets** for all sensitive Firebase data
3. **Rotate service account keys** periodically
4. **Monitor access logs** in Firebase Console
5. **Use minimal required permissions** for service accounts

## Troubleshooting

### FCM notifications not working?
1. Check if `FCM_SERVICE_ACCOUNT_JSON` secret contains valid JSON
2. Verify the service account has "Firebase Admin SDK Service Agent" role
3. Ensure `FCM_PROJECT_ID` matches your Firebase project
4. Check container logs: `docker-compose -f docker-compose.prod.yml logs api`

### File not found errors?
1. Verify GitHub secrets are correctly formatted (no extra spaces/characters)
2. Check if files were created: `ls -la ~/coptic-app-backend/firebase/`
3. Verify Docker volume mount: `docker exec coptic_api_prod ls -la /app/firebase/`

### Permission denied errors?
1. Check file permissions: `ls -la ~/coptic-app-backend/firebase/`
2. Ensure Docker container can read mounted files
3. Verify service account has required Firebase permissions

## Testing

After deployment, test FCM functionality:

```bash
# Check if credentials are mounted correctly
docker exec coptic_api_prod ls -la /app/firebase/

# Test FCM endpoint
curl -X POST http://your-server:5000/api/user/{userId}/test-notification \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Client App Configuration

### Android
Download `google-services.json` from your server:
```bash
curl http://your-server:5000/firebase/google-services.json -o app/google-services.json
```

### iOS
Download `GoogleService-Info.plist` from your server:
```bash
curl http://your-server:5000/firebase/GoogleService-Info.plist -o ios/Runner/GoogleService-Info.plist
```

## Additional Resources

- [Firebase Admin SDK Setup](https://firebase.google.com/docs/admin/setup)
- [FCM Server Implementation](https://firebase.google.com/docs/cloud-messaging/server)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
