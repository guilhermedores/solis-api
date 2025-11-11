import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

// Middleware para extrair tenant do subdomínio e configurar CORS
export function middleware(request: NextRequest) {
  // Tratar requisições OPTIONS (CORS preflight)
  if (request.method === 'OPTIONS') {
    return new NextResponse(null, {
      status: 200,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, PATCH, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization, x-tenant, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Date, X-Api-Version',
        'Access-Control-Max-Age': '86400',
      },
    })
  }

  // Pegar o hostname da request
  const hostname = request.headers.get('host') || ''
  
  // Desenvolvimento local - usar header ou query param para simular tenant
  const isDevelopment = process.env.NODE_ENV === 'development'
  
  let tenant = ''
  
  if (isDevelopment) {
    // Em desenvolvimento, aceitar tenant via header ou query
    tenant = request.headers.get('x-tenant') || 
             request.nextUrl.searchParams.get('tenant') || 
             'default'
  } else {
    // Em produção, extrair do subdomínio
    // Formato: cliente1.solis.com.br ou cliente1.localhost:3000
    const subdomain = hostname.split('.')[0]
    
    // Lista de subdomínios reservados que não são tenants
    const reservedSubdomains = ['www', 'api', 'admin', 'app', 'localhost']
    
    if (reservedSubdomains.includes(subdomain)) {
      tenant = 'default'
    } else {
      tenant = subdomain
    }
  }
  
  // Criar response com header do tenant e CORS
  const response = NextResponse.next()
  
  // Headers CORS
  response.headers.set('Access-Control-Allow-Origin', '*')
  response.headers.set('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, PATCH, OPTIONS')
  response.headers.set('Access-Control-Allow-Headers', 'Content-Type, Authorization, x-tenant, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Date, X-Api-Version')
  
  // Header do tenant
  response.headers.set('x-tenant', tenant)
  
  // Também adicionar o tenant nos cookies para facilitar acesso client-side
  if (tenant !== 'default') {
    response.cookies.set('tenant', tenant, {
      httpOnly: false,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      maxAge: 60 * 60 * 24 * 365 // 1 ano
    })
  }
  
  return response
}

// Configurar quais rotas o middleware deve processar
export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public files (public directory)
     */
    '/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
}
