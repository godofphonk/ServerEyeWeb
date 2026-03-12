# Doppler Setup Script for Production
# This script helps configure Doppler for ServerEye production environment

#!/bin/bash

set -e

echo "🚀 Setting up Doppler for ServerEye Production..."

# Check if Doppler CLI is installed
if ! command -v doppler &> /dev/null; then
    echo "📦 Installing Doppler CLI..."
    curl -Ls --tlsv1.2 --proto "=https" --fail https://cli.doppler.com/install.sh | sh
    export PATH="$PATH:$HOME/.local/bin"
fi

# Check if user is logged in to Doppler
if ! doppler whoami &> /dev/null; then
    echo "🔐 Please login to Doppler:"
    doppler login
fi

# Set up the project
echo "📋 Setting up Doppler project..."
DOPPLER_PROJECT=${DOPPLER_PROJECT:-servereye}
DOPPLER_CONFIG=${DOPPLER_CONFIG:-production}

echo "Project: $DOPPLER_PROJECT"
echo "Config: $DOPPLER_CONFIG"

# Create secrets from template file
echo "🔧 Creating secrets from template..."
if [ -f "./environments/prod/doppler-secrets.env" ]; then
    echo "⚠️  Please update the values in ./environments/prod/doppler-secrets.env"
    echo "⚠️  Then run: doppler secrets import --config=$DOPPLER_CONFIG ./environments/prod/doppler-secrets.env"
else
    echo "❌ Template file not found: ./environments/prod/doppler-secrets.env"
    exit 1
fi

# Generate JWT RSA keys if not exist
echo "🔑 Generating JWT RSA keys..."
if [ ! -f "./environments/prod/jwt-private.pem" ]; then
    openssl genrsa -out ./environments/prod/jwt-private.pem 2048
    openssl rsa -in ./environments/prod/jwt-private.pem -pubout -out ./environments/prod/jwt-public.pem
    
    # Convert to base64 for environment variables
    PRIVATE_KEY_BASE64=$(base64 -w 0 ./environments/prod/jwt-private.pem)
    PUBLIC_KEY_BASE64=$(base64 -w 0 ./environments/prod/jwt-public.pem)
    
    echo "📝 JWT Private Key Base64: $PRIVATE_KEY_BASE64"
    echo "📝 JWT Public Key Base64: $PUBLIC_KEY_BASE64"
    
    echo "⚠️  Update these values in your Doppler configuration!"
fi

# Generate encryption key
echo "🔐 Generating encryption key..."
if [ ! -f "./environments/prod/encryption-key.txt" ]; then
    openssl rand -hex 16 > ./environments/prod/encryption-key.txt
    ENCRYPTION_KEY=$(cat ./environments/prod/encryption-key.txt)
    echo "📝 Encryption Key: $ENCRYPTION_KEY"
    echo "⚠️  Update this value in your Doppler configuration!"
fi

# Generate NextAuth secret
echo "🔐 Generating NextAuth secret..."
if [ ! -f "./environments/prod/nextauth-secret.txt" ]; then
    openssl rand -base64 32 > ./environments/prod/nextauth-secret.txt
    NEXAUTH_SECRET=$(cat ./environments/prod/nextauth-secret.txt)
    echo "📝 NextAuth Secret: $NEXAUTH_SECRET"
    echo "⚠️  Update this value in your Doppler configuration!"
fi

# Instructions
echo ""
echo "✅ Doppler setup completed!"
echo ""
echo "📋 Next steps:"
echo "1. Update the values in ./environments/prod/doppler-secrets.env with real values"
echo "2. Import secrets to Doppler:"
echo "   doppler secrets import --config=$DOPPLER_CONFIG ./environments/prod/doppler-secrets.env"
echo "3. Test the configuration:"
echo "   doppler run --config=$DOPPLER_CONFIG -- printenv"
echo "4. Deploy with Docker Compose:"
echo "   doppler run --config=$DOPPLER_CONFIG -- docker-compose -f ./environments/prod/docker-compose.yml up -d"
echo ""
echo "🔧 Useful Doppler commands:"
echo "  doppler secrets --config=$DOPPLER_CONFIG                    # List all secrets"
echo "  doppler secrets get SECRET_NAME --config=$DOPPLER_CONFIG   # Get specific secret"
echo "  doppler run --config=$DOPPLER_CONFIG -- <command>          # Run command with secrets"
echo "  doppler secrets download --config=$DOPPLER_CONFIG > .env   # Download secrets to file"
