# Development Dockerfile for Frontend (Next.js)
# Optimized for development with hot reload

FROM node:20-alpine AS base
WORKDIR /app

# Install dependencies
FROM base AS deps
RUN apk add --no-cache libc6-compat
COPY package*.json ./
RUN npm install

# Development stage
FROM base AS dev
WORKDIR /app

# Copy node_modules from deps
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Install curl for health checks
RUN apk add --no-cache curl

# Expose port
EXPOSE 3000

# Environment variables
ENV NODE_ENV=development \
    NEXT_TELEMETRY_DISABLED=1 \
    NODE_OPTIONS="--max-old-space-size=4096"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:3000 || exit 1

# Start development server
CMD ["npm", "run", "dev"]
