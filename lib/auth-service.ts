/**
 * Serviço de autenticação e validação de tokens
 */

import jwt from 'jsonwebtoken'
import { getPrismaClient, prismaPublic } from './prisma'

export interface TokenPayload {
  empresaId: string
  tenantId: string
  tenant: string
  type: string
  agentName: string
  iat: number
  exp: number
  iss: string
  aud: string
}

export interface ValidateTokenResult {
  valid: boolean
  payload?: TokenPayload
  error?: string
  empresa?: {
    id: string
    razaoSocial: string
    nomeFantasia: string | null
    cnpj: string
    ativo: boolean
  }
  tenant?: {
    id: string
    subdomain: string
    active: boolean
  }
}

export class AuthService {
  /**
   * Valida um token JWT e retorna informações da empresa e tenant
   */
  static async validateToken(token: string): Promise<ValidateTokenResult> {
    try {
      const jwtSecret = process.env.JWT_SECRET || 'your-super-secret-jwt-key-change-in-production'

      // Verifica e decodifica o token
      const decoded = jwt.verify(token, jwtSecret, {
        issuer: 'solis-api',
        audience: 'solis-agente',
      }) as TokenPayload

      // Valida se é um token de agente
      if (decoded.type !== 'agente-pdv') {
        return {
          valid: false,
          error: 'Tipo de token inválido'
        }
      }

      // Busca o tenant no schema public
      const tenant = await prismaPublic.tenant.findUnique({
        where: { id: decoded.tenantId }
      })

      if (!tenant) {
        return {
          valid: false,
          error: 'Tenant não encontrado'
        }
      }

      // Valida se o tenant está ativo
      if (!tenant.active) {
        return {
          valid: false,
          error: 'Tenant está inativo'
        }
      }

      // Busca a empresa no schema do tenant
      const tenantPrisma = await getPrismaClient(decoded.tenant)
      const empresa = await tenantPrisma.empresa.findUnique({
        where: { id: decoded.empresaId }
      })

      if (!empresa) {
        return {
          valid: false,
          error: 'Empresa não encontrada'
        }
      }

      // Valida se a empresa está ativa
      if (!empresa.ativo) {
        return {
          valid: false,
          error: 'Empresa está inativa'
        }
      }

      return {
        valid: true,
        payload: decoded,
        empresa: {
          id: empresa.id,
          razaoSocial: empresa.razaoSocial,
          nomeFantasia: empresa.nomeFantasia,
          cnpj: empresa.cnpj,
          ativo: empresa.ativo
        },
        tenant: {
          id: tenant.id,
          subdomain: tenant.subdomain,
          active: tenant.active
        }
      }

    } catch (error) {
      if (error instanceof jwt.TokenExpiredError) {
        return {
          valid: false,
          error: 'Token expirado'
        }
      }

      if (error instanceof jwt.JsonWebTokenError) {
        return {
          valid: false,
          error: 'Token inválido'
        }
      }

      return {
        valid: false,
        error: 'Erro ao validar token: ' + (error instanceof Error ? error.message : 'Erro desconhecido')
      }
    }
  }

  /**
   * Extrai informações básicas de um token sem validar no banco
   * Útil para debug ou logging
   */
  static decodeToken(token: string): TokenPayload | null {
    try {
      const decoded = jwt.decode(token) as TokenPayload
      return decoded
    } catch (error) {
      return null
    }
  }

  /**
   * Gera um token de autenticação para uma empresa
   */
  static async generateToken(
    empresaId: string,
    tenantId: string,
    expiresInDays: number = 3650,
    agentName?: string
  ): Promise<{ token: string; expiresAt: Date } | null> {
    try {
      // Valida o tenant
      const tenant = await prismaPublic.tenant.findUnique({
        where: { subdomain: tenantId }
      })

      if (!tenant || !tenant.active) {
        return null
      }

      // Busca a empresa no schema do tenant
      const tenantPrisma = await getPrismaClient(tenantId)
      const empresa = await tenantPrisma.empresa.findUnique({
        where: { id: empresaId }
      })

      if (!empresa || !empresa.ativo) {
        return null
      }

      const jwtSecret = process.env.JWT_SECRET || 'your-super-secret-jwt-key-change-in-production'
      const expiresAt = new Date()
      expiresAt.setDate(expiresAt.getDate() + expiresInDays)

      const token = jwt.sign(
        {
          empresaId: empresa.id,
          tenantId: tenant.id,
          tenant: tenant.subdomain,
          type: 'agente-pdv',
          agentName: agentName || 'Agente PDV',
          iat: Math.floor(Date.now() / 1000),
        },
        jwtSecret,
        {
          expiresIn: `${expiresInDays}d`,
          issuer: 'solis-api',
          audience: 'solis-agente',
        }
      )

      return { token, expiresAt }
    } catch (error) {
      console.error('[AuthService] Erro ao gerar token:', error)
      return null
    }
  }
}
