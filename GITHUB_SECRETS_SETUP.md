# GitHub Secrets Setup for AWS Cognito

To automatically deploy AWS Cognito credentials without exposing them in your code, you need to set up GitHub Secrets.

## Required GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these 5 secrets:

### 1. AWS_ACCESS_KEY_ID
```
[Your AWS Access Key ID from local.env]
```

### 2. AWS_SECRET_ACCESS_KEY
```
[Your AWS Secret Access Key from local.env]
```

### 3. AWS_REGION
```
eu-north-1
```

### 4. COGNITO_USER_POOL_ID
```
[Your Cognito User Pool ID from local.env]
```

### 5. COGNITO_CLIENT_ID
```
[Your Cognito Client ID from local.env]
```

**Note:** Use the same values from your working `local.env` file.

## How It Works

1. **GitHub Secrets** store your AWS credentials securely
2. **CI/CD workflow** automatically injects these values during deployment
3. **Your server** gets the credentials without manual setup
4. **Every deployment** automatically includes the latest credentials

## Benefits

- ✅ **Secure**: Credentials never appear in your code
- ✅ **Automatic**: No manual setup needed after each push
- ✅ **Consistent**: Same credentials on every deployment
- ✅ **No maintenance**: Set once, works forever

## Setup Steps

1. Go to: https://github.com/YohannesTesfaye21/coptic_backend/settings/secrets/actions
2. Click **"New repository secret"**
3. Add each of the 5 secrets above
4. Push any change to trigger deployment
5. Your server will automatically have AWS credentials!

## Verification

After setting up secrets and pushing changes:

```bash
# SSH into your server
ssh -i ~/.ssh/id_ed25519 root@162.243.165.212

# Check if credentials are properly deployed
cd ~/coptic-app-backend
cat .env | grep AWS

# Check if containers are using AWS
docker logs coptic_api_prod | grep -i "AWS\|Cognito"
```

You should see AWS credentials in the `.env` file and AwsCognitoService logs instead of SimpleAuthService.