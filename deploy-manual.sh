#!/bin/bash

echo "🚀 Starting manual deployment..."

# Check if GitHub token is provided
if [ -z "$GITHUB_TOKEN" ]; then
    echo "❌ Error: GITHUB_TOKEN environment variable is required"
    echo "Please set it with: export GITHUB_TOKEN=your_github_token_here"
    echo "You can get a token from: https://github.com/settings/tokens"
    exit 1
fi

# Ensure SSH access is preserved in firewall
echo "🛡️  Configuring firewall safely..."
ufw allow ssh || ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 5000/tcp
ufw --force enable

# Verify SSH is still accessible
echo "🔍 Verifying SSH access..."
ufw status | grep -E "(22|ssh)" || echo "⚠️  SSH port not found in firewall rules"

# Create app directory if it doesn't exist
mkdir -p ~/coptic-app-backend
cd ~/coptic-app-backend

# Authenticate with GitHub Container Registry using proper token
echo "🔐 Authenticating with GitHub Container Registry..."
echo "$GITHUB_TOKEN" | docker login ghcr.io -u yohannestesfaye21 --password-stdin

# Copy necessary files to server
echo "📁 Copying deployment files..."
if [ -f "docker-compose.prod.yml" ]; then
  cp docker-compose.prod.yml ~/coptic-app-backend/
fi
if [ -f "nginx-simple.conf" ]; then
  cp nginx-simple.conf ~/coptic-app-backend/
fi
if [ -f "env.production" ]; then
  cp env.production ~/coptic-app-backend/
fi

# Copy environment file
echo "📋 Setting up environment..."
if [ -f "env.production" ]; then
  cp env.production .env
else
  echo "⚠️  env.production not found, creating basic .env"
  echo "# Database Configuration" > .env
  echo "POSTGRES_DB=coptic_app" >> .env
  echo "POSTGRES_USER=coptic_user" >> .env
  echo "POSTGRES_PASSWORD=your_secure_password_here" >> .env
  echo "" >> .env
  echo "# JWT Configuration" >> .env
  echo "JWT_KEY=this-is-a-very-long-secret-key-for-jwt-signing-that-is-at-least-64-characters-long-for-security" >> .env
  echo "JWT_ISSUER=coptic-app-backend" >> .env
  echo "JWT_AUDIENCE=coptic-app-frontend" >> .env
  echo "JWT_EXPIRY_HOURS=24" >> .env
  echo "" >> .env
  echo "# Redis Configuration" >> .env
  echo "REDIS_PASSWORD=your_redis_password_here" >> .env
  echo "" >> .env
  echo "# Application Configuration" >> .env
  echo "ASPNETCORE_ENVIRONMENT=Production" >> .env
fi

# Pull latest changes if git repo exists
if [ -d ".git" ]; then
  echo "📥 Pulling latest changes..."
  git pull origin main
else
  echo "📁 No git repo found, continuing with deployment"
fi

# Re-authenticate before pulling images to ensure permissions persist
echo "🔐 Re-authenticating with GitHub Container Registry..."
echo "$GITHUB_TOKEN" | docker login ghcr.io -u yohannestesfaye21 --password-stdin

# Pull latest Docker images
echo "🐳 Pulling latest Docker images..."
docker-compose -f docker-compose.prod.yml pull

# Stop existing services
echo "🛑 Stopping existing services..."
docker-compose -f docker-compose.prod.yml down

# Start services with new images
echo "🚀 Starting services..."
docker-compose -f docker-compose.prod.yml up -d

# Wait for services to be healthy
echo "⏳ Waiting for services to start..."
sleep 30

# Check service status
echo "🔍 Checking service status..."
docker-compose -f docker-compose.prod.yml ps

# Run database migrations if needed
echo "🗄️  Running database migrations..."
docker-compose -f docker-compose.prod.yml exec -T api dotnet ef database update || echo "Migrations completed or no migrations needed"

# Configure Nginx if needed
echo "🌐 Configuring Nginx..."
if ! command -v nginx &> /dev/null; then
  echo "📦 Installing Nginx..."
  apt update && apt install -y nginx
fi

if [ ! -f /etc/nginx/sites-enabled/coptic-app ]; then
  echo "📝 Setting up Nginx configuration..."
  if [ -f "nginx-simple.conf" ]; then
    cp nginx-simple.conf /etc/nginx/sites-available/coptic-app
    ln -sf /etc/nginx/sites-available/coptic-app /etc/nginx/sites-enabled/
    nginx -t && systemctl reload nginx
  else
    echo "⚠️  nginx-simple.conf not found, skipping Nginx setup"
  fi
fi

echo "✅ Deployment completed successfully!"
echo "🌐 Your app should be accessible at:"
echo "   - Direct: http://$(hostname -I | awk '{print $1}'):5000"
echo "   - Proxy: http://$(hostname -I | awk '{print $1}')"
echo "   - Swagger: http://$(hostname -I | awk '{print $1}'):5000/swagger"
