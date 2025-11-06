import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

// Middleware para extrair tenant do subdomínio
export function middleware(request: NextRequest) {
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
  
  // Criar response com header do tenant
  const response = NextResponse.next()
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
