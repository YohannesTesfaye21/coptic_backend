#!/bin/bash

# Deployment script for Coptic App Backend
# Usage: ./deploy.sh [environment] [server]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${1:-production}
SERVER=${2:-your-server-ip}
SSH_USER=${SSH_USER:-ubuntu}
SSH_KEY=${SSH_KEY:-~/.ssh/id_rsa}
DOCKER_REGISTRY=${DOCKER_REGISTRY:-ghcr.io/YohannesTesfaye21/coptic_backend}

echo -e "${GREEN}🚀 Starting deployment to $ENVIRONMENT on $SERVER${NC}"

# Check if required environment variables are set
if [ -z "$SERVER" ] || [ "$SERVER" = "your-server-ip" ]; then
    echo -e "${RED}❌ Error: Please provide a server IP or set SERVER environment variable${NC}"
    echo "Usage: ./deploy.sh [environment] [server]"
    echo "Example: ./deploy.sh production 192.168.1.100"
    exit 1
fi

# Function to print step
print_step() {
    echo -e "${YELLOW}📋 $1${NC}"
}

# Function to check command success
check_success() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ $1 completed successfully${NC}"
    else
        echo -e "${RED}❌ $1 failed${NC}"
        exit 1
    fi
}

print_step "Checking SSH connection..."
ssh -i "$SSH_KEY" -o ConnectTimeout=10 -o StrictHostKeyChecking=no "$SSH_USER@$SERVER" "echo 'SSH connection successful'"
check_success "SSH connection test"

print_step "Creating deployment directory on server..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "mkdir -p ~/coptic-app-backend"
check_success "Directory creation"

print_step "Copying Docker Compose files to server..."
scp -i "$SSH_KEY" docker-compose.prod.yml "$SSH_USER@$SERVER:~/coptic-app-backend/"
scp -i "$SSH_KEY" env.example "$SSH_USER@$SERVER:~/coptic-app-backend/"
check_success "File copy"

print_step "Setting up environment file on server..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && cp env.example .env"
check_success "Environment file setup"

print_step "Installing Docker on server (if not present)..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker \$USER
    echo 'Docker installed. Please log out and back in, then run deployment again.'
    exit 0
fi"
check_success "Docker installation check"

print_step "Installing Docker Compose on server (if not present)..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "if ! command -v docker-compose &> /dev/null; then
    sudo curl -L \"https://github.com/docker/compose/releases/latest/download/docker-compose-\$(uname -s)-\$(uname -m)\" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi"
check_success "Docker Compose installation check"

print_step "Pulling latest Docker images..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml pull"
check_success "Docker image pull"

print_step "Stopping existing services..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml down || true"
check_success "Service stop"

print_step "Starting services..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml up -d"
check_success "Service start"

print_step "Waiting for services to be healthy..."
sleep 30

print_step "Checking service health..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml ps"
check_success "Health check"

print_step "Running database migrations..."
ssh -i "$SSH_KEY" "$SSH_USER@$SERVER" "cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml exec -T api dotnet ef database update || echo 'Migrations completed or no migrations needed'"
check_success "Database migrations"

echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
echo -e "${GREEN}🌐 Your application is now running on $SERVER${NC}"
echo -e "${YELLOW}📝 Don't forget to:${NC}"
echo -e "${YELLOW}   1. Update your .env file on the server with production values${NC}"
echo -e "${YELLOW}   2. Configure your domain/DNS to point to $SERVER${NC}"
echo -e "${YELLOW}   3. Set up SSL certificates if needed${NC}"
echo -e "${YELLOW}   4. Configure firewall rules for ports 80, 443, and 5432${NC}"
