/**
 * API de estatísticas do Tenant
 * GET /api/tenants/:id/stats - Retorna estatísticas do tenant
 */

import { NextRequest, NextResponse } from 'next/server'
import { tenantService } from '@/lib/tenant-service'

interface RouteParams {
  params: {
    id: string
  }
}

/**
 * GET /api/tenants/:id/stats
 * Retorna estatísticas do tenant (terminais, plano, etc)
 */
export async function GET(request: NextRequest, { params }: RouteParams) {
  try {
    const stats = await tenantService.getTenantStats(params.id)
    
    return NextResponse.json(stats)
  } catch (error: any) {
    console.error('Erro ao buscar estatísticas:', error)
    
    if (error.message.includes('não encontrado')) {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao buscar estatísticas' },
      { status: 500 }
    )
  }
}
