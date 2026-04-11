# Production Dockerfile for ServerEye Frontend
# Multi-stage build for optimized production image

# Stage 1: Dependencies
FROM node:20.12.2-alpine AS deps
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache && apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci --only=production && npm cache clean --force

# Stage 2: Builder
FROM node:20.12.2-alpine AS builder
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache
WORKDIR /app

ARG NEXT_PUBLIC_API_URL
ARG NEXT_PUBLIC_WS_URL
ARG NEXT_PUBLIC_COOKIE_DOMAIN
ARG SECRETS_HASH
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL} \
    NEXT_PUBLIC_WS_URL=${NEXT_PUBLIC_WS_URL} \
    NEXT_PUBLIC_COOKIE_DOMAIN=${NEXT_PUBLIC_COOKIE_DOMAIN}

COPY package.json package-lock.json* ./
RUN npm ci --prefer-offline --no-audit

COPY . .
# Invalidate cache when secrets change by using SECRETS_HASH in a dummy RUN
RUN echo "Secrets hash: ${SECRETS_HASH}"
RUN npm run build:production

# Stage 3: Runner
FROM node:20.12.2-alpine AS runner
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache dumb-init && \
    rm -rf /var/lib/apk/lists/*

WORKDIR /app

ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL} \
    NODE_ENV=production \
    PORT=3000 \
    NEXT_TELEMETRY_DISABLED=1

# Create non-root user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy necessary files from builder
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

USER nextjs

EXPOSE 3000

ENV HOSTNAME="0.0.0.0" \
    NODE_OPTIONS="--max-old-space-size=4096"

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000 || exit 1

ENTRYPOINT ["dumb-init", "--"]
CMD ["node", "server.js"]
