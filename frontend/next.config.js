/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  images: {
    domains: [],
  },
  // Enable fast refresh
  reactStrictMode: false,
  // Optimize for development
  swcMinify: true,
  // Fast refresh options
  experimental: {
    optimizePackageImports: ['lucide-react', 'framer-motion'],
  },
};

module.exports = nextConfig;
