# Production Dockerfile for ServerEye Frontend
# Multi-stage build for optimized production image

# Stage 1: Dependencies
FROM node:20-alpine AS deps
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache && apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --only=production

# Stage 2: Builder
FROM node:20-alpine AS builder
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build:production

# Stage 3: Runner
FROM node:20-alpine AS runner
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000

# Create non-root user
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy necessary files from builder
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

USER nextjs

EXPOSE 3000

ENV HOSTNAME="0.0.0.0"
CMD ["node", "server.js"]
