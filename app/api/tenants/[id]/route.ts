/**
 * API de gerenciamento de Tenant específico
 * GET    /api/tenants/:id          - Busca tenant por ID
 * PUT    /api/tenants/:id          - Atualiza tenant
 * DELETE /api/tenants/:id          - Desativa tenant (soft delete)
 * GET    /api/tenants/:id/stats    - Estatísticas do tenant
 * POST   /api/tenants/:id/reactivate - Reativa tenant
 */

import { NextRequest, NextResponse } from 'next/server'
import { tenantService, UpdateTenantInput } from '@/lib/tenant-service'

interface RouteParams {
  params: {
    id: string
  }
}

/**
 * GET /api/tenants/:id
 * Busca tenant por ID
 */
export async function GET(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await tenantService.getTenantById(params.id)
    
    if (!tenant) {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(tenant)
  } catch (error) {
    console.error('Erro ao buscar tenant:', error)
    return NextResponse.json(
      { error: 'Erro ao buscar tenant' },
      { status: 500 }
    )
  }
}

/**
 * PUT /api/tenants/:id
 * Atualiza tenant
 */
export async function PUT(request: NextRequest, { params }: RouteParams) {
  try {
    const body = await request.json() as UpdateTenantInput
    
    const tenant = await tenantService.updateTenant(params.id, body)
    
    return NextResponse.json(tenant)
  } catch (error: any) {
    console.error('Erro ao atualizar tenant:', error)
    
    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao atualizar tenant' },
      { status: 500 }
    )
  }
}

/**
 * DELETE /api/tenants/:id
 * Desativa tenant (soft delete)
 */
export async function DELETE(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await tenantService.deactivateTenant(params.id)
    
    return NextResponse.json({
      message: 'Tenant desativado com sucesso',
      tenant,
    })
  } catch (error: any) {
    console.error('Erro ao desativar tenant:', error)
    
    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao desativar tenant' },
      { status: 500 }
    )
  }
}
