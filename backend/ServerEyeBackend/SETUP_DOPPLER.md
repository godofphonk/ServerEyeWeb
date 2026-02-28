# Doppler Setup Instructions for ServerEye Backend

## 🚀 Quick Setup

### 1. Install Doppler CLI

**Option A: With sudo (recommended)**
```bash
curl -Ls --tlsv1.2 --proto "=https" --write-out "Downloaded doppler CLI\n" --fail https://cli.doppler.com/install.sh | sh
```

**Option B: Manual installation**
```bash
# Download and extract
wget https://github.com/DopplerHQ/cli/releases/download/v3.75.3/doppler-linux-amd64.tar.gz
tar -xzf doppler-linux-amd64.tar.gz
sudo mv doppler /usr/local/bin/
```

**Option C: Snap**
```bash
sudo snap install doppler
```

### 2. Login to Doppler
```bash
doppler login
```

### 3. Setup Project
```bash
# Navigate to project directory
cd /home/gospodin/Рабочий\ стол/homeProjects/ServerEyeProjects/ServerEyeWeb/backend/ServerEyeBackend

# Import secrets from template
doppler import --template .doppler/doppler-template.yaml

# Or create secrets manually
doppler secrets set JWT_SECRET_KEY "your-super-secret-jwt-key-change-in-production-32-chars"
doppler secrets set ENCRYPTION_KEY "your-32-character-encryption-key-here-change-in-prod"
# ... add all other secrets
```

### 4. Configure Environments
```bash
# Create environments
doppler environments create dev
doppler environments create staging
doppler environments create production

# Switch to dev environment
doppler switch dev
```

### 5. Generate Production Keys
```bash
# Generate RSA keys for JWT
openssl genpkey -algorithm RSA -out private_key.pem -pkcs8 -pass pass:yourpassphrase 2048
openssl rsa -pubout -in private_key.pem -out public_key.pem

# Convert to base64 for Doppler
base64 -w 0 private_key.pem
base64 -w 0 public_key.pem

# Set in Doppler
doppler secrets set JWT_PRIVATE_KEY_BASE64 "$(base64 -w 0 private_key.pem)"
doppler secrets set JWT_PUBLIC_KEY_BASE64 "$(base64 -w 0 public_key.pem)"
```

## 🔧 Integration with .NET

### 1. Add Doppler NuGet Package
```bash
dotnet add package Doppler.Sdk
```

### 2. Update Program.cs
```csharp
// Add at the beginning of Program.cs
if (builder.Environment.IsProduction())
{
    var doppler = new DopplerApi();
    var secrets = await doppler.GetSecretsAsync("servereye", "production");
    
    builder.Configuration.AddInMemoryCollection(secrets.ToDictionary(
        s => s.Key,
        s => s.Value
    ));
}
```

### 3. Update Dockerfile
```dockerfile
# Add Doppler CLI
RUN curl -Ls --tlsv1.2 --proto "=https" --write-out "Downloaded doppler CLI\n" --fail https://cli.doppler.com/install.sh | sh

# Run with Doppler secrets
ENTRYPOINT ["doppler", "run", "--", "dotnet", "ServerEye.API.dll"]
```

### 4. Update docker-compose.yml
```yaml
services:
  backend:
    environment:
      - DOPPLER_PROJECT=servereye
      - DOPPLER_CONFIG=production
    # Remove all secret environment variables
```

## 📋 Required Secrets Checklist

### 🔐 Security Secrets
- [ ] `JWT_SECRET_KEY` - JWT signing key (32+ chars)
- [ ] `JWT_PRIVATE_KEY_BASE64` - RSA private key for production
- [ ] `JWT_PUBLIC_KEY_BASE64` - RSA public key for production
- [ ] `ENCRYPTION_KEY` - Data encryption key (32+ chars)

### 🗄️ Database Secrets
- [ ] `DATABASE_CONNECTION_STRING` - PostgreSQL connection string
- [ ] `TICKET_DB_CONNECTION_STRING` - Ticket database connection string
- [ ] `REDIS_CONNECTION_STRING` - Redis connection string

### 📧 Email Secrets
- [ ] `AWS_ACCESS_KEY` - AWS SES access key
- [ ] `AWS_SECRET_KEY` - AWS SES secret key
- [ ] `SMTP_USERNAME` - SMTP username
- [ ] `SMTP_PASSWORD` - SMTP password

### 🌐 API Secrets
- [ ] `GO_API_BASE_URL` - Go API base URL
- [ ] `GO_API_PRODUCTION_URL` - Production Go API URL

## 🚀 Deployment Commands

### Development
```bash
doppler run -- dotnet run
```

### Production
```bash
doppler run --config production -- dotnet run
```

### Docker
```bash
doppler run --config production -- docker-compose up -d
```

## 🔍 Verification

### Check Secrets
```bash
doppler secrets list
doppler secrets get JWT_SECRET_KEY
```

### Test Integration
```bash
# Test with Doppler secrets
doppler run -- dotnet run --urls=http://localhost:5000

# Test API endpoints
curl http://localhost:5000/api/users/me
```

## 📝 Next Steps

1. ✅ Install Doppler CLI
2. ✅ Login and setup project
3. ✅ Import secrets from template
4. ✅ Generate production RSA keys
5. 🔄 Update .NET configuration
6. 🔄 Update Docker configuration
7. 🔄 Test integration
8. 🔄 Deploy to staging

## 🆘 Troubleshooting

### Common Issues
1. **Permission denied**: Use sudo for CLI installation
2. **404 errors**: Check Doppler project name and config
3. **Missing secrets**: Run `doppler secrets list` to verify
4. **Docker issues**: Ensure Doppler token is available in container

### Support
- Doppler docs: https://docs.doppler.com
- CLI help: `doppler --help`
- Project support: Check Doppler dashboard
