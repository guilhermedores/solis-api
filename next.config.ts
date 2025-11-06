import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // API-only configuration
  reactStrictMode: true,
  
  // Disable static page generation (API only)
  output: 'standalone',
  
  // Optimize for production
  poweredByHeader: false,
  compress: true,
  
  // Logging
  logging: {
    fetches: {
      fullUrl: true,
    },
  },
  
  // Turbopack configuration (Next.js 16+)
  turbopack: {},
};

export default nextConfig;
