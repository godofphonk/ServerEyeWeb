#!/bin/bash

# RSA Key Generation Script for ServerEye
# This script generates RSA keys for JWT signing and exports them for Doppler

set -e

echo "🔐 Generating RSA Keys for ServerEye JWT"

# Create keys directory if it doesn't exist
mkdir -p keys

# Generate RSA private key
echo "📝 Generating private key..."
openssl genpkey -algorithm RSA -out keys/private_key.pem -pkcs8 -pass pass:servereye2024 2048

# Extract public key
echo "📝 Extracting public key..."
openssl rsa -pubout -in keys/private_key.pem -out keys/public_key.pem -passin pass:servereye2024

# Convert to base64 for Doppler
echo "🔄 Converting to base64..."
PRIVATE_KEY_BASE64=$(base64 -w 0 keys/private_key.pem)
PUBLIC_KEY_BASE64=$(base64 -w 0 keys/public_key.pem)

# Save keys to files
echo "💾 Saving keys..."
echo "$PRIVATE_KEY_BASE64" > keys/private_key_base64.txt
echo "$PUBLIC_KEY_BASE64" > keys/public_key_base64.txt

# Display keys (for manual setup)
echo ""
echo "🔑 Generated Keys:"
echo "=================="
echo "Private Key (base64):"
echo "$PRIVATE_KEY_BASE64"
echo ""
echo "Public Key (base64):"
echo "$PUBLIC_KEY_BASE64"
echo ""

# Doppler commands
echo "📋 Doppler Setup Commands:"
echo "=========================="
echo "# Set these secrets in Doppler:"
echo "doppler secrets set JWT_PRIVATE_KEY_BASE64 \"$PRIVATE_KEY_BASE64\""
echo "doppler secrets set JWT_PUBLIC_KEY_BASE64 \"$PUBLIC_KEY_BASE64\""
echo ""

# Instructions for updating JwtService
echo "📝 Instructions for updating JwtService:"
echo "========================================"
echo "1. Update JwtService.cs to load RSA keys from configuration"
echo "2. Use JWT_PRIVATE_KEY_BASE64 for signing"
echo "3. Use JWT_PUBLIC_KEY_BASE64 for validation"
echo ""

# Security reminder
echo "⚠️  SECURITY REMINDER:"
echo "===================="
echo "1. Keep the private key secure!"
echo "2. Add keys/*.pem to .gitignore"
echo "3. Store keys only in Doppler"
echo "4. Rotate keys regularly"
echo ""

echo "✅ Key generation complete!"
echo "Files saved in keys/ directory"
