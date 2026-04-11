/** @type {import('next').NextConfig} */
const nextConfig = {
  output: process.env.CI_E2E === 'true' ? undefined : 'standalone',
  allowedDevOrigins: ['127.0.0.1'],
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'api.servereye.dev',
      },
      {
        protocol: 'https',
        hostname: 'cdn.servereye.dev',
      },
    ],
    formats: ['image/webp', 'image/avif'],
  },
  // Enable React Strict Mode for production
  reactStrictMode: true,
  // Security headers
  async headers() {
    const isDevelopment = process.env.NODE_ENV === 'development';

    return [
      {
        source: '/(.*)',
        headers: [
          {
            key: 'X-Frame-Options',
            value: 'DENY',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          {
            key: 'Referrer-Policy',
            value: 'origin-when-cross-origin',
          },
          {
            key: 'Permissions-Policy',
            value: 'camera=(), microphone=(), geolocation=()',
          },
          // Content Security Policy - different for development and production
          {
            key: 'Content-Security-Policy',
            value: isDevelopment
              ? "default-src 'self' 'unsafe-inline' 'unsafe-eval'; connect-src 'self' ws: wss: http://localhost:* https://localhost:* http://127.0.0.1:* https://127.0.0.1:* http://backend:* https: https://telegram.org; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://telegram.org; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: https:; font-src 'self' data:; frame-src 'self' https://telegram.org; frame-ancestors 'none';"
              : "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://telegram.org; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https: https://telegram.org; frame-src 'self' https://telegram.org; frame-ancestors 'none';",
          },
        ],
      },
    ];
  },
  // Optimize package imports
  experimental: {
    optimizePackageImports: ['lucide-react', 'framer-motion', 'recharts'],
    scrollRestoration: true,
  },
  // Turbopack configuration
  turbopack: {
    // Empty config to prevent webpack conflicts
  },
  // Compression
  compress: true,
  // Development optimizations
  ...(process.env.NODE_ENV === 'development' && {
    compiler: {
      removeConsole: false,
    },
  }),

  // Production optimizations
  poweredByHeader: false,
  generateEtags: true,
  // Environment variables
  env: {
    CUSTOM_KEY: process.env.CUSTOM_KEY,
  },
};

export default nextConfig;
