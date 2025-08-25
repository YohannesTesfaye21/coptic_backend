# Docker Setup Guide

This guide explains how to set up and run the Coptic App Backend using Docker and Docker Compose.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (usually comes with Docker Desktop)
- Git

## Quick Start

### 1. Clone and Navigate to Project
```bash
git clone <your-repo-url>
cd coptic_app_backend
```

### 2. Environment Setup
Copy the example environment file and configure it:
```bash
cp env.example .env
# Edit .env with your actual values
```

### 3. Start Development Environment
```bash
# Using Makefile (recommended)
make dev

# Or manually
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

### 4. Access the Application
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/health

## Available Commands

### Using Makefile
```bash
make help          # Show all available commands
make build         # Build Docker images
make up            # Start services
make down          # Stop services
make restart       # Restart services
make logs          # Show logs from all services
make logs-api      # Show API logs
make logs-db       # Show database logs
make clean         # Clean up containers and volumes
make test          # Run tests
make dev           # Start development environment
make prod          # Start production environment
make shell         # Open shell in API container
make db-shell      # Open database shell
```

### Using Docker Compose Directly
```bash
# Development
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Production
docker-compose -f docker-compose.prod.yml up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Development vs Production

### Development Environment
- Uses `docker-compose.yml` + `docker-compose.override.yml`
- Hot reload with `dotnet watch`
- Exposed ports for debugging
- Development database password

### Production Environment
- Uses `docker-compose.prod.yml`
- Optimized for performance
- Environment variables from `.env` file
- Health checks enabled
- Secure database configuration

## Environment Variables

Create a `.env` file based on `env.example`:

```bash
# Database Configuration
POSTGRES_DB=coptic_app
POSTGRES_USER=coptic_user
POSTGRES_PASSWORD=your_secure_password_here

# JWT Configuration
JWT_KEY=your_jwt_secret_key_here
JWT_ISSUER=coptic-app-backend
JWT_AUDIENCE=coptic-app-frontend
JWT_EXPIRY_HOURS=24

# Redis Configuration
REDIS_PASSWORD=your_redis_password_here

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
```

## Database Management

### Access Database
```bash
# Using Makefile
make db-shell

# Or manually
docker-compose exec postgres psql -U coptic_user -d coptic_app
```

### Run Migrations
```bash
# Inside the API container
docker-compose exec api dotnet ef database update
```

### Reset Database
```bash
# Stop services and remove volumes
make clean

# Start fresh
make up
```

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   ```bash
   # Check what's using the port
   lsof -i :5000
   
   # Stop conflicting services
   docker-compose down
   ```

2. **Database Connection Issues**
   ```bash
   # Check database logs
   make logs-db
   
   # Restart database
   docker-compose restart postgres
   ```

3. **Build Failures**
   ```bash
   # Clean and rebuild
   make clean
   make build
   ```

### Logs and Debugging
```bash
# View all logs
make logs

# View specific service logs
make logs-api
make logs-db

# Follow logs in real-time
docker-compose logs -f api
```

## CI/CD Integration

The project includes GitHub Actions workflows that:
- Build and test on every push to main
- Build Docker images
- Push to GitHub Container Registry
- Deploy to production (configurable)

### GitHub Actions Secrets
Set these secrets in your GitHub repository:
- `DOCKER_USERNAME`: Docker registry username
- `DOCKER_PASSWORD`: Docker registry password
- `DEPLOY_SSH_KEY`: SSH key for deployment server

## Performance Optimization

### Production Tips
1. Use production Docker Compose file
2. Set appropriate environment variables
3. Enable health checks
4. Use volume mounts for persistent data
5. Configure proper logging levels

### Resource Limits
You can add resource limits to services in `docker-compose.prod.yml`:

```yaml
services:
  api:
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'
```

## Security Considerations

1. **Never commit `.env` files**
2. **Use strong passwords** for database and Redis
3. **Rotate JWT keys** regularly
4. **Limit exposed ports** in production
5. **Use secrets management** for sensitive data

## Monitoring and Health Checks

The application includes health check endpoints:
- `/health` - Basic health status
- Database connectivity checks
- Service dependency verification

## Support

For issues or questions:
1. Check the logs: `make logs`
2. Review this documentation
3. Check GitHub Issues
4. Contact the development team
