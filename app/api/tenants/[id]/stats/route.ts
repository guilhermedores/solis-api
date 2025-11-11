/**
 * API de informações do Tenant
 * GET /api/tenants/:id/stats - Retorna informações do tenant
 */

import { NextRequest, NextResponse } from 'next/server'
import { tenantService } from '@/lib/tenant-service'

interface RouteParams {
  params: Promise<{
    id: string
  }>
}

/**
 * GET /api/tenants/:id/stats
 * Retorna informações do tenant (plano, limites, etc)
 */
export async function GET(request: NextRequest, { params }: RouteParams) {
  try {
    const { id } = await params
    const tenant = await tenantService.getTenantById(id)
    
    if (!tenant) {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json({
      id: tenant.id,
      subdomain: tenant.subdomain,
      companyName: tenant.companyName,
      plan: tenant.plan,
      maxTerminals: tenant.maxTerminals,
      maxUsers: tenant.maxUsers,
      active: tenant.active,
      features: tenant.features,
    })
  } catch (error: any) {
    console.error('Erro ao buscar informações do tenant:', error)
    
    return NextResponse.json(
      { error: 'Erro ao buscar informações do tenant' },
      { status: 500 }
    )
  }
}
