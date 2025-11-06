/**
 * API de reativação de Tenant
 * POST /api/tenants/:id/reactivate - Reativa tenant desativado
 */

import { NextRequest, NextResponse } from 'next/server'
import { tenantService } from '@/lib/tenant-service'

interface RouteParams {
  params: {
    id: string
  }
}

/**
 * POST /api/tenants/:id/reactivate
 * Reativa tenant que foi desativado
 */
export async function POST(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await tenantService.reactivateTenant(params.id)
    
    return NextResponse.json({
      message: 'Tenant reativado com sucesso',
      tenant,
    })
  } catch (error: any) {
    console.error('Erro ao reativar tenant:', error)
    
    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao reativar tenant' },
      { status: 500 }
    )
  }
}
