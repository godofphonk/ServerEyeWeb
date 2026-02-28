# ServerEye Backend - Production Ready

🚀 **Enterprise-level .NET backend with Doppler secrets management, JWT authentication, and production-ready infrastructure**

## 📋 Overview

ServerEye Backend is a modern ASP.NET Core application designed for production deployment with enterprise-level security, monitoring, and scalability features.

### ✅ Key Features Implemented

- 🔐 **Doppler Secrets Management** - Centralized secret management
- 🛡️ **JWT RSA Authentication** - Production-ready token system
- 🗄️ **PostgreSQL + Redis** - Scalable data storage
- 🐳 **Docker Compose** - Containerized deployment
- 📊 **Health Checks** - Application monitoring
- 🔄 **Environment Configuration** - Dev/Staging/Prod support
- 🚦 **Rate Limiting** - DDoS protection
- 📝 **Structured Logging** - Production logging
- 🔧 **Global Exception Handling** - Centralized error management

## 🏗️ Architecture

```
ServerEye Backend/
├── ServerEye.API/           # Web API layer
├── ServerEye.Core/          # Business logic & entities
├── ServerEye.Infrastracture/ # Data access & external services
├── scripts/                # Deployment & utility scripts
├── .doppler/               # Doppler configuration
└── docker-compose.Doppler.yml # Container orchestration
```

## 🚀 Quick Start

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- [Doppler CLI](https://docs.doppler.com/docs/install-cli)
- PostgreSQL client tools (optional)

### 1. Setup Doppler
```bash
# Install Doppler CLI
curl -Ls --tlsv1.2 --proto "=https" --write-out "Downloaded doppler CLI\n" --fail https://cli.doppler.com/install.sh | sh

# Login and setup project
doppler login
doppler setup --project servereye
```

### 2. Generate RSA Keys
```bash
# Generate production keys
./scripts/generate-keys.sh

# Set keys in Doppler
doppler secrets set JWT_PRIVATE_KEY_BASE64 "$(cat keys/private_key_base64.txt)"
doppler secrets set JWT_PUBLIC_KEY_BASE64 "$(cat keys/public_key_base64.txt)"
```

### 3. Configure Secrets
```bash
# Import all secrets from template
doppler import --template .doppler/doppler-template.yaml

# Or configure manually
doppler secrets set DATABASE_CONNECTION_STRING "Host=postgres;Port=5432;Database=ServerEyeWeb;Username=postgres;Password=postgres"
doppler secrets set REDIS_CONNECTION_STRING "redis:6379"
```

### 4. Deploy
```bash
# Development
docker-compose -f docker-compose.Doppler.yml up -d

# Production
docker-compose -f docker-compose.Doppler.yml --profile production up -d
```

## 📚 Documentation

- [📖 Full Setup Guide](./SETUP_DOPPLER.md)
- [🚀 Deployment Instructions](./DEPLOYMENT.md)
- [🔧 Configuration Reference](./docs/CONFIGURATION.md)
- [🔍 Troubleshooting Guide](./docs/TROUBLESHOOTING.md)

## 🔧 Development

### Local Development
```bash
# Clone repository
git clone <repository-url>
cd ServerEyeBackend

# Setup environment
cp .env.example .env
# Edit .env with your values

# Run with Doppler secrets
doppler run -- dotnet run

# Or with Docker
docker-compose -f docker-compose.Doppler.yml up backend
```

### Project Structure
```
ServerEye.API/
├── Controllers/           # API endpoints
├── Extensions/           # Configuration extensions
├── Middleware/          # Custom middleware
├── Services/            # API-specific services
├── Validators/          # FluentValidation validators
└── Configuration/       # Settings classes

ServerEye.Core/
├── Entities/            # Domain entities
├── DTOs/               # Data transfer objects
├── Enums/              # Application enums
├── Interfaces/         # Service interfaces
└── Services/           # Business logic services

ServerEye.Infrastracture/
├── Repositories/       # Data access layer
├── ExternalServices/   # Third-party integrations
├── Caching/           # Redis caching
├── Migrations/         # Database migrations
└── Persistens/        # Entity configurations
```

## 🐳 Docker Deployment

### Development Environment
```bash
# Start all services
docker-compose -f docker-compose.Doppler.yml up -d

# View logs
docker-compose -f docker-compose.Doppler.yml logs -f backend

# Stop services
docker-compose -f docker-compose.Doppler.yml down
```

### Production Environment
```bash
# Deploy production services
docker-compose -f docker-compose.Doppler.yml --profile production up -d

# Scale backend service
docker-compose -f docker-compose.Doppler.yml --profile production up -d --scale backend=3
```

## 📊 Monitoring

### Health Checks
```bash
# Basic health check
curl http://localhost:5246/health

# Detailed health check
curl http://localhost:5246/health/ready

# Liveness probe
curl http://localhost:5246/health/live
```

### Application Logs
```bash
# View container logs
docker logs -f servereyeWeb-backend

# View logs with Doppler
doppler run -- docker logs -f servereyeWeb-backend
```

## 🔐 Security Features

### Authentication & Authorization
- JWT tokens with RSA signing
- Refresh token rotation
- Role-based authorization
- Email verification

### Security Headers
- HTTPS enforcement
- CORS configuration
- Rate limiting
- Input validation

### Secrets Management
- Doppler integration
- No hardcoded secrets
- Environment-specific configuration
- Key rotation support

## 🚀 API Endpoints

### Authentication
```
POST /api/auth/refresh          # Refresh access token
POST /api/auth/logout           # Logout user
POST /api/auth/revoke           # Revoke refresh token
POST /api/auth/verify-email     # Verify email address
POST /api/auth/forgot-password  # Request password reset
POST /api/auth/reset-password   # Reset password
```

### Users
```
GET  /api/users                 # Get all users
GET  /api/users/me              # Get current user
POST /api/users/register        # Register new user
POST /api/users/login           # User login
PUT  /api/users/{id}            # Update user
DELETE /api/users/{id}          # Delete user
```

### Servers
```
GET  /api/servers               # Get servers list
GET  /api/monitored-servers    # Get user's servers
POST /api/monitored-servers/add # Add server
DELETE /api/monitored-servers/{id} # Remove server
```

### Metrics
```
GET  /api/metrics/{serverId}/latest # Get latest metrics
GET  /api/server-metrics/{serverId}  # Get server metrics
```

## 🧪 Testing

### Unit Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test ServerEye.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
# Run integration tests
dotnet test ServerEye.IntegrationTests

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Testing dotnet test ServerEye.IntegrationTests
```

## 🔄 CI/CD

### GitHub Actions
The project includes GitHub Actions workflows for:
- Automated testing
- Security scanning
- Docker image building
- Deployment to staging/production

### Deployment Pipeline
1. **Push to main** → Run tests
2. **Merge to develop** → Deploy to staging
3. **Tag release** → Deploy to production

## 📈 Performance

### Database Optimization
- Connection pooling
- Query optimization
- Indexing strategy
- Caching layer

### Application Performance
- Response compression
- Static file caching
- Memory optimization
- Async operations

### Monitoring Metrics
- Request latency
- Error rates
- Database performance
- Cache hit rates

## 🔧 Configuration

### Environment Variables
See `.env.example` for all available configuration options.

### Doppler Secrets
All sensitive data is stored in Doppler:
- Database connection strings
- JWT keys
- API keys
- Email credentials

### Application Settings
Configuration is loaded in this order:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Doppler secrets
4. Environment variables
5. Command line arguments

## 🆘 Support

### Documentation
- [Doppler Docs](https://docs.doppler.com)
- [.NET Docs](https://docs.microsoft.com/dotnet)
- [PostgreSQL Docs](https://www.postgresql.org/docs/)
- [Redis Docs](https://redis.io/documentation)

### Common Issues
See [TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md) for common problems and solutions.

### Getting Help
- Create an issue in the repository
- Check the documentation
- Review the logs for error messages

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 🚀 Roadmap

- [ ] API versioning
- [ ] OpenTelemetry integration
- [ ] GraphQL support
- [ ] Advanced caching strategies
- [ ] Multi-tenant support
- [ ] Real-time notifications

---

**Built with ❤️ for production deployment**
