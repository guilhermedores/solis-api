import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // API-only configuration
  reactStrictMode: true,
  
  // Disable static page generation (API only)
  output: 'standalone',
  
  // Optimize for production
  poweredByHeader: false,
  compress: true,
  
  // CORS headers globais
  async headers() {
    return [
      {
        // Aplicar a todas as rotas da API
        source: '/api/:path*',
        headers: [
          { key: 'Access-Control-Allow-Credentials', value: 'true' },
          { key: 'Access-Control-Allow-Origin', value: '*' },
          { key: 'Access-Control-Allow-Methods', value: 'GET,DELETE,PATCH,POST,PUT,OPTIONS' },
          { key: 'Access-Control-Allow-Headers', value: 'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version, Authorization, x-tenant' },
        ],
      },
    ]
  },
  
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
