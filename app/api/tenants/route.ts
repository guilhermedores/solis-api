/**
 * API de gerenciamento de Tenants
 * GET    /api/tenants       - Lista todos os tenants
 * GET    /api/tenants/:id   - Busca tenant por ID
 * POST   /api/tenants       - Cria novo tenant
 * PUT    /api/tenants/:id   - Atualiza tenant
 * DELETE /api/tenants/:id   - Deleta tenant (soft delete)
 */

import { NextRequest, NextResponse } from 'next/server'
import { tenantService, CreateTenantInput, TenantFilter } from '@/lib/tenant-service'

/**
 * GET /api/tenants
 * Lista tenants com filtros opcionais
 */
export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams
    
    // Filtros
    const filter: TenantFilter = {}
    const activeParam = searchParams.get('active')
    if (activeParam !== null) {
      filter.active = activeParam === 'true'
    }
    
    const plan = searchParams.get('plan')
    if (plan) {
      filter.plan = plan
    }
    
    const subdomain = searchParams.get('subdomain')
    if (subdomain) {
      filter.subdomain = subdomain
    }
    
    // Paginação
    const page = parseInt(searchParams.get('page') || '1')
    const limit = parseInt(searchParams.get('limit') || '50')
    
    const result = await tenantService.listTenants(filter, page, limit)
    
    return NextResponse.json(result)
  } catch (error) {
    console.error('Erro ao listar tenants:', error)
    return NextResponse.json(
      { error: 'Erro ao listar tenants' },
      { status: 500 }
    )
  }
}

/**
 * POST /api/tenants
 * Cria novo tenant
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json() as CreateTenantInput
    
    // Validações básicas
    if (!body.subdomain || !body.companyName) {
      return NextResponse.json(
        { error: 'Subdomain e nome da empresa são obrigatórios' },
        { status: 400 }
      )
    }
    
    const tenant = await tenantService.createTenant(body)
    
    return NextResponse.json(tenant, { status: 201 })
  } catch (error: any) {
    console.error('Erro ao criar tenant:', error)
    
    // Erros de validação ou duplicação
    if (error.message.includes('inválido') || error.message.includes('unique')) {
      return NextResponse.json(
        { error: error.message },
        { status: 400 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao criar tenant' },
      { status: 500 }
    )
  }
}
