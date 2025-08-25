#!/bin/bash

# Server setup script for Coptic App Backend
# Run this on your Ubuntu server to prepare it for deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 Server Setup Script for Coptic App Backend${NC}"
echo -e "${YELLOW}This script will prepare your Ubuntu server for deployment${NC}"
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then
    echo -e "${RED}❌ Please don't run this script as root. Use a regular user with sudo privileges.${NC}"
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

# Function to run command with sudo
run_sudo() {
    echo -e "${BLUE}Running: $1${NC}"
    sudo $1
    check_success "$2"
}

print_step "Updating system packages..."
run_sudo "apt update && apt upgrade -y" "System update"

print_step "Installing essential packages..."
run_sudo "apt install -y curl wget git unzip software-properties-common apt-transport-https ca-certificates gnupg lsb-release" "Essential packages installation"

print_step "Installing Docker..."
if ! command -v docker &> /dev/null; then
    # Add Docker's official GPG key
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
    
    # Add Docker repository
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
    
    # Install Docker
    run_sudo "apt update" "Docker repository update"
    run_sudo "apt install -y docker-ce docker-ce-cli containerd.io" "Docker installation"
    
    # Add user to docker group
    sudo usermod -aG docker $USER
    echo -e "${YELLOW}⚠️  Docker installed. You need to log out and back in for group changes to take effect.${NC}"
else
    echo -e "${GREEN}✅ Docker is already installed${NC}"
fi

print_step "Installing Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    # Install Docker Compose
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
    
    # Create symlink for compatibility
    sudo ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose
    check_success "Docker Compose installation"
else
    echo -e "${GREEN}✅ Docker Compose is already installed${NC}"
fi

print_step "Installing Nginx (for reverse proxy and SSL termination)..."
if ! command -v nginx &> /dev/null; then
    run_sudo "apt install -y nginx" "Nginx installation"
    
    # Start and enable Nginx
    run_sudo "systemctl start nginx" "Nginx start"
    run_sudo "systemctl enable nginx" "Nginx enable"
else
    echo -e "${GREEN}✅ Nginx is already installed${NC}"
fi

print_step "Installing Certbot for SSL certificates..."
if ! command -v certbot &> /dev/null; then
    run_sudo "apt install -y certbot python3-certbot-nginx" "Certbot installation"
else
    echo -e "${GREEN}✅ Certbot is already installed${NC}"
fi

print_step "Configuring firewall..."
# Install UFW if not present
if ! command -v ufw &> /dev/null; then
    run_sudo "apt install -y ufw" "UFW installation"
fi

# Configure firewall
run_sudo "ufw allow ssh" "SSH firewall rule"
run_sudo "ufw allow 80/tcp" "HTTP firewall rule"
run_sudo "ufw allow 443/tcp" "HTTPS firewall rule"
run_sudo "ufw allow 5432/tcp" "PostgreSQL firewall rule"
run_sudo "ufw --force enable" "Firewall enable"

print_step "Creating application directory..."
mkdir -p ~/coptic-app-backend
check_success "Application directory creation"

print_step "Setting up Nginx configuration..."
sudo tee /etc/nginx/sites-available/coptic-app-backend > /dev/null <<EOF
server {
    listen 80;
    server_name _;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    location /chatHub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

# Enable the site
sudo ln -sf /etc/nginx/sites-available/coptic-app-backend /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Test Nginx configuration
run_sudo "nginx -t" "Nginx configuration test"

# Reload Nginx
run_sudo "systemctl reload nginx" "Nginx reload"

print_step "Setting up log rotation..."
sudo tee /etc/logrotate.d/coptic-app-backend > /dev/null <<EOF
/home/*/coptic-app-backend/logs/*.log {
    daily
    missingok
    rotate 52
    compress
    delaycompress
    notifempty
    create 644 root root
    postrotate
        systemctl reload nginx
    endscript
}
EOF

print_step "Creating systemd service for auto-restart..."
sudo tee /etc/systemd/system/coptic-app-backend.service > /dev/null <<EOF
[Unit]
Description=Coptic App Backend
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/home/$USER/coptic-app-backend
ExecStart=/usr/local/bin/docker-compose -f docker-compose.prod.yml up -d
ExecStop=/usr/local/bin/docker-compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

# Enable the service
run_sudo "systemctl daemon-reload" "Systemd daemon reload"
run_sudo "systemctl enable coptic-app-backend.service" "Service enable"

print_step "Setting up monitoring..."
# Install htop for system monitoring
run_sudo "apt install -y htop" "htop installation"

# Create monitoring script
tee ~/monitor.sh > /dev/null <<EOF
#!/bin/bash
echo "=== System Status ==="
echo "Date: \$(date)"
echo "Uptime: \$(uptime)"
echo ""
echo "=== Docker Services ==="
cd ~/coptic-app-backend
docker-compose -f docker-compose.prod.yml ps
echo ""
echo "=== System Resources ==="
free -h
echo ""
echo "=== Disk Usage ==="
df -h
echo ""
echo "=== Recent Logs ==="
docker-compose -f docker-compose.prod.yml logs --tail=20
EOF

chmod +x ~/monitor.sh

echo ""
echo -e "${GREEN}🎉 Server setup completed successfully!${NC}"
echo ""
echo -e "${YELLOW}📝 Next steps:${NC}"
echo -e "${YELLOW}   1. Log out and log back in for Docker group changes to take effect${NC}"
echo -e "${YELLOW}   2. Copy your docker-compose.prod.yml and .env files to ~/coptic-app-backend/${NC}"
echo -e "${YELLOW}   3. Update the .env file with your production values${NC}"
echo -e "${YELLOW}   4. Run: cd ~/coptic-app-backend && docker-compose -f docker-compose.prod.yml up -d${NC}"
echo -e "${YELLOW}   5. Set up your domain DNS to point to this server${NC}"
echo -e "${YELLOW}   6. Run: sudo certbot --nginx -d yourdomain.com${NC}"
echo ""
echo -e "${BLUE}🔍 Useful commands:${NC}"
echo -e "${BLUE}   ~/monitor.sh          - Monitor system and services${NC}"
echo -e "${BLUE}   sudo systemctl status coptic-app-backend.service${NC}"
echo -e "${BLUE}   docker-compose -f docker-compose.prod.yml logs -f${NC}"
echo -e "${BLUE}   sudo ufw status       - Check firewall status${NC}"
