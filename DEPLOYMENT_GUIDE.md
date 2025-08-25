# 🚀 Production Deployment Guide

This guide will walk you through deploying your Coptic App Backend to a production server.

## 📋 Prerequisites

- A Ubuntu 20.04+ server with SSH access
- A domain name pointing to your server
- SSH key pair for secure access
- Basic knowledge of Linux commands

## 🖥️ Server Requirements

### Minimum Requirements
- **CPU**: 1 cores
- **RAM**: 2GB
- **Storage**: 50GB SSD
- **OS**: Ubuntu 20.04 LTS or newer

### Recommended Requirements
- **CPU**: 4+ cores
- **RAM**: 8GB+
- **Storage**: 50GB+ SSD
- **OS**: Ubuntu 22.04 LTS

## 🔧 Step 1: Server Setup

### Option A: Automated Setup (Recommended)
1. **SSH into your server:**
   ```bash
   ssh your-username@your-server-ip
   ```

2. **Download and run the setup script:**
   ```bash
   wget https://raw.githubusercontent.com/YohannesTesfaye21/coptic_backend/main/server-setup.sh
   chmod +x server-setup.sh
   ./server-setup.sh
   ```

3. **Log out and back in** for Docker group changes to take effect.

### Option B: Manual Setup
If you prefer manual setup, follow these steps:

1. **Update system:**
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

2. **Install Docker:**
   ```bash
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   sudo usermod -aG docker $USER
   ```

3. **Install Docker Compose:**
   ```bash
   sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
   sudo chmod +x /usr/local/bin/docker-compose
   ```

4. **Install Nginx:**
   ```bash
   sudo apt install -y nginx certbot python3-certbot-nginx
   ```

5. **Configure firewall:**
   ```bash
   sudo ufw allow ssh
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw --force enable
   ```

## 📁 Step 2: Application Setup

1. **Create application directory:**
   ```bash
   mkdir -p ~/coptic-app-backend
   cd ~/coptic-app-backend
   ```

2. **Copy configuration files:**
   ```bash
   # Copy from your local machine
   scp docker-compose.prod.yml your-username@your-server-ip:~/coptic-app-backend/
   scp env.production your-username@your-server-ip:~/coptic-app-backend/
   ```

3. **Set up environment file:**
   ```bash
   cp env.production .env
   nano .env  # Edit with your actual values
   ```

## 🔐 Step 3: Environment Configuration

### Critical Security Settings
Update your `.env` file with secure values:

```bash
# Generate a strong JWT key (at least 64 characters)
JWT_KEY=$(openssl rand -base64 64)

# Generate a strong database password (at least 32 characters)
POSTGRES_PASSWORD=$(openssl rand -base64 32)

# Generate a strong Redis password
REDIS_PASSWORD=$(openssl rand -base64 32)
```

### Required Environment Variables
- `POSTGRES_PASSWORD`: Strong database password
- `JWT_KEY`: Very long JWT signing key
- `REDIS_PASSWORD`: Strong Redis password
- `ALLOWED_HOSTS`: Your domain names
- `CORS_ORIGINS`: Allowed frontend origins

## 🐳 Step 4: Docker Deployment

1. **Start the application:**
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

2. **Check service status:**
   ```bash
   docker-compose -f docker-compose.prod.yml ps
   ```

3. **View logs:**
   ```bash
   docker-compose -f docker-compose.prod.yml logs -f
   ```

## 🌐 Step 5: Domain and SSL Setup

1. **Point your domain** to your server's IP address

2. **Update Nginx configuration** with your domain:
   ```bash
   sudo nano /etc/nginx/sites-available/coptic-app-backend
   ```
   
   Replace `server_name _;` with `server_name yourdomain.com www.yourdomain.com;`

3. **Reload Nginx:**
   ```bash
   sudo nginx -t
   sudo systemctl reload nginx
   ```

4. **Obtain SSL certificate:**
   ```bash
   sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com
   ```

## 🔄 Step 6: CI/CD Setup

### GitHub Actions Configuration

1. **Add repository secrets** in GitHub:
   - Go to your repository → Settings → Secrets and variables → Actions
   - Add these secrets:
     - `SERVER_HOST`: Your server IP address
     - `SERVER_USER`: SSH username
     - `SERVER_SSH_KEY`: Your private SSH key
     - `SERVER_PORT`: SSH port (usually 22)

2. **The workflow will automatically:**
   - Build and test on every push to main
   - Deploy to your server
   - Run database migrations
   - Restart services

## 📊 Step 7: Monitoring and Maintenance

### Health Checks
```bash
# Check application health
curl http://localhost:5000/health

# Check service status
docker-compose -f docker-compose.prod.yml ps

# Monitor system resources
~/monitor.sh
```

### Logs
```bash
# Application logs
docker-compose -f docker-compose.prod.yml logs -f api

# Database logs
docker-compose -f docker-compose.prod.yml logs -f postgres

# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Backup
```bash
# Database backup
docker-compose -f docker-compose.prod.yml exec postgres pg_dump -U coptic_user_prod coptic_app_prod > backup_$(date +%Y%m%d_%H%M%S).sql

# File uploads backup
tar -czf uploads_backup_$(date +%Y%m%d_%H%M%S).tar.gz wwwroot/uploads/
```

## 🚨 Troubleshooting

### Common Issues

1. **Port already in use:**
   ```bash
   sudo netstat -tulpn | grep :80
   sudo systemctl stop nginx  # if needed
   ```

2. **Permission denied:**
   ```bash
   sudo chown -R $USER:$USER ~/coptic-app-backend
   ```

3. **Database connection failed:**
   ```bash
   docker-compose -f docker-compose.prod.yml logs postgres
   docker-compose -f docker-compose.prod.yml restart postgres
   ```

4. **SSL certificate issues:**
   ```bash
   sudo certbot renew --dry-run
   sudo certbot --nginx -d yourdomain.com
   ```

### Performance Issues

1. **Check resource usage:**
   ```bash
   htop
   docker stats
   ```

2. **Optimize database:**
   ```bash
   # Check slow queries
   docker-compose -f docker-compose.prod.yml exec postgres psql -U coptic_user_prod -d coptic_app_prod -c "SELECT * FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"
   ```

## 🔒 Security Best Practices

1. **Firewall configuration:**
   - Only expose necessary ports (22, 80, 443)
   - Use UFW for easy management

2. **Regular updates:**
   ```bash
   sudo apt update && sudo apt upgrade -y
   docker system prune -f
   ```

3. **SSL/TLS:**
   - Use Let's Encrypt for free certificates
   - Enable automatic renewal

4. **Database security:**
   - Use strong passwords
   - Limit database access to application only
   - Regular backups

## 📈 Scaling Considerations

### Vertical Scaling
- Increase server resources (CPU, RAM, Storage)
- Optimize Docker container resource limits

### Horizontal Scaling
- Use load balancer (HAProxy, Nginx)
- Multiple application instances
- Database clustering (PostgreSQL with read replicas)

## 📞 Support

If you encounter issues:

1. **Check logs first:**
   ```bash
   docker-compose -f docker-compose.prod.yml logs
   ```

2. **Verify configuration:**
   ```bash
   docker-compose -f docker-compose.prod.yml config
   ```

3. **Restart services:**
   ```bash
   docker-compose -f docker-compose.prod.yml restart
   ```

4. **Check GitHub Issues** for known problems

## 🎯 Quick Deployment Checklist

- [ ] Server setup completed
- [ ] Environment variables configured
- [ ] Docker services running
- [ ] Domain DNS configured
- [ ] SSL certificate obtained
- [ ] GitHub Actions secrets configured
- [ ] Health checks passing
- [ ] Monitoring set up
- [ ] Backup strategy implemented

## 🚀 Next Steps

After successful deployment:

1. **Test all API endpoints** using Swagger UI
2. **Monitor application performance** and logs
3. **Set up automated backups**
4. **Configure monitoring alerts**
5. **Plan for future updates and scaling**

Your application is now running in production! 🎉
