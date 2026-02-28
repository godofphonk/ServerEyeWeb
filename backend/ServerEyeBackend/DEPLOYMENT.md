# ServerEye Backend Deployment Guide

## 🚀 Quick Start with Doppler

### Prerequisites
- Docker and Docker Compose installed
- Doppler account and CLI
- Generated RSA keys for JWT

### Step 1: Setup Doppler
```bash
# Install Doppler CLI
curl -Ls --tlsv1.2 --proto "=https" --write-out "Downloaded doppler CLI\n" --fail https://cli.doppler.com/install.sh | sh

# Login to Doppler
doppler login

# Setup project (if not already done)
doppler setup --project servereye
```

### Step 2: Generate RSA Keys
```bash
# Generate keys for production
cd /home/gospodin/Рабочий\ стол/homeProjects/ServerEyeProjects/ServerEyeWeb/backend/ServerEyeBackend
./scripts/generate-keys.sh

# Set keys in Doppler
doppler secrets set JWT_PRIVATE_KEY_BASE64 "$(cat keys/private_key_base64.txt)"
doppler secrets set JWT_PUBLIC_KEY_BASE64 "$(cat keys/public_key_base64.txt)"
```

### Step 3: Configure Secrets
```bash
# Import all secrets from template
doppler import --template .doppler/doppler-template.yaml

# Or set secrets manually
doppler secrets set DATABASE_CONNECTION_STRING "Host=postgres;Port=5432;Database=ServerEyeWeb;Username=postgres;Password=postgres"
doppler secrets set TICKET_DB_CONNECTION_STRING "Host=postgres-ticket;Port=5432;Database=ServerEyeWeb-ticket;Username=postgres;Password=postgres"
doppler secrets set REDIS_CONNECTION_STRING "redis:6379"
# ... add other secrets
```

### Step 4: Deploy with Docker Compose
```bash
# Development deployment
docker-compose -f docker-compose.Doppler.yml up -d

# Production deployment
docker-compose -f docker-compose.Doppler.yml --profile production up -d
```

## 📋 Environment Configuration

### Development Environment
```bash
# Switch to dev config
doppler switch dev

# Run locally with Doppler secrets
doppler run -- dotnet run

# Or with Docker
docker-compose -f docker-compose.Doppler.yml up backend
```

### Staging Environment
```bash
# Create staging config
doppler environments create staging
doppler switch staging

# Configure staging secrets
doppler secrets set DATABASE_CONNECTION_STRING "staging-db-connection-string"
# ... other staging secrets

# Deploy staging
docker-compose -f docker-compose.Doppler.yml up -d
```

### Production Environment
```bash
# Switch to production config
doppler switch production

# Configure production secrets
doppler secrets set DATABASE_CONNECTION_STRING "production-db-connection-string"
doppler secrets set REDIS_CONNECTION_STRING "production-redis-connection-string"
# ... other production secrets

# Deploy production
docker-compose -f docker-compose.Doppler.yml --profile production up -d
```

## 🔧 Configuration Files

### appsettings.json (Base Configuration)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Production.json (Production Overrides)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "Security": {
    "RequireHttps": true,
    "EnableRateLimiting": true,
    "EnableAuditLogging": true
  }
}
```

## 🐳 Docker Configuration

### Multi-Stage Build
The `Dockerfile.Doppler` uses multi-stage builds for optimization:
1. **Base**: ASP.NET runtime
2. **Build**: .NET SDK for compilation
3. **Publish**: Optimized publish
4. **Final**: Production image with Doppler CLI

### Health Checks
```bash
# Check container health
docker ps
curl http://localhost:5246/health

# View logs
docker logs servereyeWeb-backend
```

## 🔍 Monitoring and Debugging

### Doppler Secrets Management
```bash
# List all secrets
doppler secrets list

# Get specific secret
doppler secrets get JWT_SECRET_KEY

# Update secret
doppler secrets set JWT_SECRET_KEY "new-secret-key"

# Sync secrets to environment
doppler run -- printenv | grep JWT
```

### Application Logs
```bash
# View application logs
docker logs -f servereyeWeb-backend

# View logs with Doppler
doppler run -- docker logs -f servereyeWeb-backend
```

### Database Connection Testing
```bash
# Test database connectivity
doppler run -- dotnet run --urls=http://localhost:5000

# Check database connection strings
doppler secrets get DATABASE_CONNECTION_STRING
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Doppler Token Not Found
```bash
# Check if token is set
echo $DOPPLER_TOKEN

# Set token
export DOPPLER_TOKEN=your_token_here

# Or login again
doppler login
```

#### 2. RSA Key Issues
```bash
# Regenerate keys
./scripts/generate-keys.sh

# Validate keys
doppler secrets get JWT_PRIVATE_KEY_BASE64
doppler secrets get JWT_PUBLIC_KEY_BASE64
```

#### 3. Database Connection Issues
```bash
# Check database connection string
doppler secrets get DATABASE_CONNECTION_STRING

# Test connection manually
doppler run -- psql "$DATABASE_CONNECTION_STRING"
```

#### 4. Redis Connection Issues
```bash
# Check Redis connection
doppler run -- redis-cli -u "$REDIS_CONNECTION_STRING" ping
```

### Debug Mode
```bash
# Run with debug logging
doppler run -- dotnet run --urls=http://localhost:5000 --verbosity debug

# Or with Docker
docker-compose -f docker-compose.Doppler.yml up --force-recreate backend
```

## 🔄 CI/CD Integration

### GitHub Actions Example
```yaml
name: Deploy ServerEye Backend
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Doppler
        run: |
          curl -Ls --tlsv1.2 --proto "=https" --write-out "Downloaded doppler CLI\n" --fail https://cli.doppler.com/install.sh | sh
          doppler login --token=${{ secrets.DOPPLER_TOKEN }}
          
      - name: Deploy with Docker Compose
        run: |
          docker-compose -f docker-compose.Doppler.yml --profile production up -d
        env:
          DOPPLER_PROD_TOKEN: ${{ secrets.DOPPLER_PROD_TOKEN }}
```

## 📊 Performance Monitoring

### Health Endpoints
- `/health` - Basic health check
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Metrics
- Application metrics via structured logging
- Database connection pool metrics
- Redis cache hit rates
- JWT token validation metrics

## 🔐 Security Best Practices

1. **Secrets Management**
   - Never commit secrets to git
   - Use Doppler for all secrets
   - Rotate keys regularly

2. **Network Security**
   - Use HTTPS in production
   - Configure proper CORS
   - Enable rate limiting

3. **Container Security**
   - Use non-root user
   - Minimal base images
   - Security scanning

## 📞 Support

- Doppler Documentation: https://docs.doppler.com
- .NET Documentation: https://docs.microsoft.com/dotnet
- Docker Documentation: https://docs.docker.com

## 🚀 Next Steps

1. ✅ Setup Doppler project
2. ✅ Generate and configure RSA keys
3. ✅ Configure all secrets
4. ✅ Deploy with Docker Compose
5. 🔄 Setup monitoring and alerting
6. 🔄 Configure CI/CD pipeline
7. 🔄 Setup backup and disaster recovery
