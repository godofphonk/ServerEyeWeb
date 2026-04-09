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

ARG NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_BASE_URL=${NEXT_PUBLIC_API_BASE_URL}

COPY package.json package-lock.json* ./
RUN npm ci --prefer-offline --no-audit

COPY . .
RUN npm run build:production

# Stage 3: Runner
FROM node:20.12.2-alpine AS runner
# Update packages for security fixes
RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache dumb-init curl ca-certificates gnupg && \
    curl -sLf --retry 3 --tlsv1.2 --proto "=https" \
        'https://cli.doppler.com/install.sh' \
        -o /tmp/install.sh && \
    sh /tmp/install.sh && \
    rm -rf /var/lib/apk/lists/* /tmp/* /var/tmp/*

WORKDIR /app

ARG NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_BASE_URL=${NEXT_PUBLIC_API_BASE_URL} \
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

ENTRYPOINT ["dumb-init", "--", "doppler", "run", "--"]
CMD ["node", "server.js"]
