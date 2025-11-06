/**
 * Serviço de gerenciamento de Tenants
 * Controla empresas/clientes no sistema multi-tenant
 */

import { PrismaClient } from '@prisma/client'
import { prismaPublic } from './prisma'

// Tipos dinâmicos para evitar problemas de cache do TypeScript
type Tenant = any
type TenantWithRelations = any

export interface CreateTenantInput {
  subdomain: string
  companyName: string
  cnpj?: string
  plan?: string
  maxTerminals?: number
  maxUsers?: number
  features?: Record<string, any>
}

export interface UpdateTenantInput {
  companyName?: string
  cnpj?: string
  active?: boolean
  plan?: string
  maxTerminals?: number
  maxUsers?: number
  features?: Record<string, any>
}

export interface TenantFilter {
  active?: boolean
  plan?: string
  subdomain?: string
}

export class TenantService {
  private prisma: any // Usando any para evitar problemas de cache do TypeScript

  constructor() {
    this.prisma = prismaPublic
  }

  /**
   * Criar novo tenant
   */
  async createTenant(data: CreateTenantInput): Promise<Tenant> {
    // Validar subdomain (apenas letras, números e hífen)
    const subdomainRegex = /^[a-z0-9-]+$/
    if (!subdomainRegex.test(data.subdomain)) {
      throw new Error('Subdomain inválido. Use apenas letras minúsculas, números e hífen.')
    }

    // Validar CNPJ se fornecido
    if (data.cnpj) {
      const cnpjLimpo = data.cnpj.replace(/[^\d]/g, '')
      if (cnpjLimpo.length !== 14) {
        throw new Error('CNPJ inválido.')
      }
    }

    const tenant = await this.prisma.tenant.create({
      data: {
        subdomain: data.subdomain.toLowerCase(),
        companyName: data.companyName,
        cnpj: data.cnpj,
        plan: data.plan || 'basic',
        maxTerminals: data.maxTerminals || 1,
        maxUsers: data.maxUsers || 5,
        features: data.features || {},
      },
    })

    return tenant
  }

  /**
   * Buscar tenant por ID
   */
  async getTenantById(id: string): Promise<Tenant | null> {
    return await this.prisma.tenant.findUnique({
      where: { id },
      include: {
        tokenVinculacoes: {
          where: { ativo: true },
          orderBy: { createdAt: 'desc' },
        },
      },
    })
  }

  /**
   * Buscar tenant por subdomain
   */
  async getTenantBySubdomain(subdomain: string): Promise<Tenant | null> {
    return await this.prisma.tenant.findUnique({
      where: { 
        subdomain: subdomain.toLowerCase(),
      },
    })
  }

  /**
   * Listar tenants com filtros
   */
  async listTenants(filter?: TenantFilter, page: number = 1, limit: number = 50): Promise<{
    data: Tenant[]
    total: number
    page: number
    totalPages: number
  }> {
    const where: any = {}

    if (filter?.active !== undefined) {
      where.active = filter.active
    }

    if (filter?.plan) {
      where.plan = filter.plan
    }

    if (filter?.subdomain) {
      where.subdomain = {
        contains: filter.subdomain.toLowerCase(),
      }
    }

    const [data, total] = await Promise.all([
      this.prisma.tenant.findMany({
        where,
        skip: (page - 1) * limit,
        take: limit,
        orderBy: { createdAt: 'desc' },
      }),
      this.prisma.tenant.count({ where }),
    ])

    return {
      data,
      total,
      page,
      totalPages: Math.ceil(total / limit),
    }
  }

  /**
   * Atualizar tenant
   */
  async updateTenant(id: string, data: UpdateTenantInput): Promise<Tenant> {
    return await this.prisma.tenant.update({
      where: { id },
      data: {
        ...data,
        updatedAt: new Date(),
      },
    })
  }

  /**
   * Desativar tenant (soft delete)
   */
  async deactivateTenant(id: string): Promise<Tenant> {
    return await this.prisma.tenant.update({
      where: { id },
      data: {
        active: false,
        deletedAt: new Date(),
      },
    })
  }

  /**
   * Reativar tenant
   */
  async reactivateTenant(id: string): Promise<Tenant> {
    return await this.prisma.tenant.update({
      where: { id },
      data: {
        active: true,
        deletedAt: null,
      },
    })
  }

  /**
   * Deletar tenant permanentemente
   */
  async deleteTenant(id: string): Promise<void> {
    await this.prisma.tenant.delete({
      where: { id },
    })
  }

  /**
   * Verificar se tenant pode adicionar mais terminais
   */
  async canAddTerminal(tenantId: string): Promise<boolean> {
    const tenant = await this.getTenantById(tenantId)
    if (!tenant || !tenant.active) {
      return false
    }

    const activeTokens = await this.prisma.tokenVinculacao.count({
      where: {
        tenantId,
        ativo: true,
        OR: [
          { validoAte: null },
          { validoAte: { gte: new Date() } },
        ],
      },
    })

    return activeTokens < tenant.maxTerminals
  }

  /**
   * Obter estatísticas do tenant
   */
  async getTenantStats(tenantId: string): Promise<{
    totalTerminals: number
    activeTerminals: number
    maxTerminals: number
    plan: string
  }> {
    const tenant = await this.getTenantById(tenantId)
    if (!tenant) {
      throw new Error('Tenant não encontrado')
    }

    const totalTerminals = await this.prisma.tokenVinculacao.count({
      where: { tenantId },
    })

    const activeTerminals = await this.prisma.tokenVinculacao.count({
      where: {
        tenantId,
        ativo: true,
        OR: [
          { validoAte: null },
          { validoAte: { gte: new Date() } },
        ],
      },
    })

    return {
      totalTerminals,
      activeTerminals,
      maxTerminals: tenant.maxTerminals,
      plan: tenant.plan,
    }
  }
}

// Instância singleton
export const tenantService = new TenantService()
