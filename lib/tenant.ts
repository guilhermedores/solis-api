// Utility para pegar o tenant da request
import { headers, cookies } from 'next/headers'

export async function getTenant(): Promise<string> {
  const headersList = await headers()
  const cookieStore = await cookies()
  
  // Primeiro tenta pegar do header (setado pelo middleware)
  const tenantFromHeader = headersList.get('x-tenant')
  
  if (tenantFromHeader && tenantFromHeader !== 'default') {
    return tenantFromHeader
  }
  
  // Se não tiver no header, tenta pegar do cookie
  const tenantFromCookie = cookieStore.get('tenant')?.value
  
  if (tenantFromCookie) {
    return tenantFromCookie
  }
  
  // Fallback para tenant default
  return 'default'
}

// Type para tenant context
export interface TenantContext {
  tenant: string
  subdomain: string
}

// Helper para extrair tenant do hostname (útil para client-side)
export function extractTenantFromHostname(hostname: string): string {
  const subdomain = hostname.split('.')[0]
  const reservedSubdomains = ['www', 'api', 'admin', 'app', 'localhost']
  
  if (reservedSubdomains.includes(subdomain)) {
    return 'default'
  }
  
  return subdomain
}
