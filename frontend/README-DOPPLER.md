# Doppler Secrets Management

## Quick Start

### 1. Run Setup Script

```bash
./setup-doppler.sh
```

### 2. Manual Setup (if script fails)

```bash
# Install Doppler CLI
curl -sL https://dl.doppler.com/install/sh | bash

# Authenticate
doppler login

# Setup project
doppler setup --project servereye-frontend --config development

# Import secrets (see DOPPLER.md for full list)
doppler secrets import .env.example --config development

# Create local .env.local
doppler secrets download --format env --config development > .env.local
```

### 3. Development

```bash
# Start development server with Doppler
npm run dev

# Or manually
doppler run --config development npm run dev:next
```

### 4. Production Build

```bash
# Build with production secrets
npm run build

# Or manually
doppler run --config production npm run build:next
```

## Required GitHub Secrets

Add these to your GitHub repository settings:

1. `DOPPLER_TOKEN` - Service account token from Doppler
2. `VERCEL_TOKEN` - Vercel deployment token (if using Vercel)
3. `VERCEL_ORG_ID` - Vercel organization ID
4. `VERCEL_PROJECT_ID` - Vercel project ID

## Environment Variables to Update in Doppler

### Production (update these in Doppler dashboard):

- `NEXTAUTH_SECRET` - Generate a strong secret
- `NEXT_PUBLIC_SENTRY_DSN` - Your Sentry DSN
- `NEXT_PUBLIC_GOOGLE_ANALYTICS_ID` - Your GA tracking ID
- `NEXT_PUBLIC_CRISP_WEBSITE_ID` - Your Crisp website ID

### Development (already set by setup script):

- `NEXT_PUBLIC_API_BASE_URL` - Local backend URL
- `NEXTAUTH_SECRET` - Development secret
- Debug flags enabled

## Common Commands

```bash
# Check Doppler status
doppler secrets status

# List secrets
doppler secrets list --config development

# Get specific secret
doppler secrets get NEXT_PUBLIC_API_BASE_URL --config development

# Sync local .env.local
npm run secrets:sync

# Audit logs
npm run secrets:audit
```

## Security Notes

✅ Never commit `.env.local` to git  
✅ Use service accounts for CI/CD  
✅ Rotate secrets regularly  
✅ Enable 2FA in Doppler  
✅ Monitor audit logs

## Troubleshooting

### Doppler command not found

```bash
export PATH="$HOME/.doppler/bin:$PATH"
```

### Permission denied

```bash
chmod +x setup-doppler.sh
```

### Secrets not loading

```bash
doppler secrets status
doppler configure get
```

### CI/CD issues

Check that `DOPPLER_TOKEN` has correct permissions for the project.
