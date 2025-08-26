# 🔐 GitHub Secrets Setup Guide

To use the GitHub Actions CI/CD pipeline, you need to set up these secrets in your repository.

## **📋 Required Secrets:**

### **1. SERVER_HOST**
- **Value**: `162.243.165.212`
- **Description**: Your server's IP address

### **2. SERVER_USER**
- **Value**: `root`
- **Description**: SSH username for your server

### **3. SERVER_SSH_KEY**
- **Value**: Your private SSH key content
- **Description**: The private key to access your server

### **4. SERVER_PORT** (Optional)
- **Value**: `22` (or `2222` if you changed SSH port)
- **Description**: SSH port for your server

## **🔧 How to Set Up Secrets:**

### **Step 1: Go to Repository Settings**
1. Open your GitHub repository
2. Click **Settings** tab
3. Click **Secrets and variables** → **Actions**

### **Step 2: Add Each Secret**
1. Click **New repository secret**
2. Enter the secret name (e.g., `SERVER_HOST`)
3. Enter the secret value
4. Click **Add secret**

### **Step 3: Get Your SSH Key Content**
```bash
# Copy your SSH private key content
cat ~/.ssh/id_ed25519
```

Copy the entire output (including `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----END OPENSSH PRIVATE KEY-----`)

## **🚀 How It Works:**

1. **Push to main branch** → Triggers workflow
2. **Build and test** → Runs locally in GitHub
3. **Deploy to server** → Uses your secrets to SSH in
4. **Automatic deployment** → Updates your app automatically

## **✅ Benefits:**

- **No manual deployment** needed
- **Automatic on every push**
- **Consistent deployment** process
- **Version control** for deployment
- **Rollback capability**

## **⚠️ Important Notes:**

- **Keep secrets secure** - never commit them to code
- **SSH key must work** - fix port 22 access first
- **Server must be accessible** from GitHub Actions
- **Environment file** gets copied automatically

## **🔍 Troubleshooting:**

### **If SSH fails:**
- Check firewall allows port 22 (or your custom port)
- Verify SSH key is correct
- Ensure server is accessible from internet

### **If deployment fails:**
- Check GitHub Actions logs
- Verify all secrets are set correctly
- Ensure server has required software (Docker, etc.)

## **📝 Next Steps:**

1. **Fix SSH access** on your server (unblock port 22)
2. **Set up GitHub secrets** using this guide
3. **Push to main branch** to trigger deployment
4. **Monitor deployment** in GitHub Actions tab

Your app will be automatically deployed every time you push changes!
