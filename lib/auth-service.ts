/**
 * Serviço de autenticação e validação de tokens
 */

import jwt from 'jsonwebtoken'
import { getPrismaClient, prismaPublic } from './prisma'

export interface TokenPayload {
  userId: string
  empresaId: string
  tenantId: string
  tenant: string
  role: string
  type: string
  iat: number
  exp: number
  iss: string
  aud: string
}

export interface ValidateTokenResult {
  valid: boolean
  payload?: TokenPayload
  error?: string
  usuario?: {
    id: string
    nome: string
    email: string
    role: string
    ativo: boolean
  }
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
   * Valida um token JWT e retorna informações do usuário, empresa e tenant
   */
  static async validateToken(token: string): Promise<ValidateTokenResult> {
    try {
      const jwtSecret = process.env.JWT_SECRET || 'your-super-secret-jwt-key-change-in-production'

      // Verifica e decodifica o token
      const decoded = jwt.verify(token, jwtSecret, {
        issuer: 'solis-api',
        audience: 'solis-pdv',
      }) as TokenPayload

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

      // Busca dados no schema do tenant
      const tenantPrisma = await getPrismaClient(decoded.tenant)
      
      // Busca o usuário
      const usuario = await tenantPrisma.usuario.findUnique({
        where: { id: decoded.userId }
      })

      if (!usuario) {
        return {
          valid: false,
          error: 'Usuário não encontrado'
        }
      }

      // Valida se o usuário está ativo
      if (!usuario.ativo) {
        return {
          valid: false,
          error: 'Usuário está inativo'
        }
      }

      // Busca a empresa
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
        usuario: {
          id: usuario.id,
          nome: usuario.nome,
          email: usuario.email,
          role: usuario.role,
          ativo: usuario.ativo
        },
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
   * Gera um token de autenticação para um usuário
   */
  static async generateToken(
    userId: string,
    empresaId: string,
    tenantId: string,
    expiresInDays: number = 30
  ): Promise<{ token: string; expiresAt: Date } | null> {
    try {
      // Valida o tenant
      const tenant = await prismaPublic.tenant.findUnique({
        where: { id: tenantId }
      })

      if (!tenant || !tenant.active) {
        return null
      }

      // Busca dados no schema do tenant
      const tenantPrisma = await getPrismaClient(tenant.subdomain)
      
      // Busca o usuário
      const usuario = await tenantPrisma.usuario.findUnique({
        where: { id: userId }
      })

      if (!usuario || !usuario.ativo) {
        return null
      }

      // Busca a empresa
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
          userId: usuario.id,
          empresaId: empresa.id,
          tenantId: tenant.id,
          tenant: tenant.subdomain,
          role: usuario.role,
          type: 'user',
          iat: Math.floor(Date.now() / 1000),
        },
        jwtSecret,
        {
          expiresIn: `${expiresInDays}d`,
          issuer: 'solis-api',
          audience: 'solis-pdv',
        }
      )

      return { token, expiresAt }
    } catch (error) {
      console.error('[AuthService] Erro ao gerar token:', error)
      return null
    }
  }

  /**
   * Gera um token de autenticação para agente PDV (sem usuário)
   */
  static async generateAgentToken(
    empresaId: string,
    tenantId: string,
    expiresInDays: number = 3650,
    agentName?: string
  ): Promise<{ token: string; expiresAt: Date } | null> {
    try {
      // Valida o tenant
      const tenant = await prismaPublic.tenant.findUnique({
        where: { id: tenantId }
      })

      if (!tenant || !tenant.active) {
        return null
      }

      // Busca a empresa no schema do tenant
      const tenantPrisma = await getPrismaClient(tenant.subdomain)
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
          userId: 'agent',
          empresaId: empresa.id,
          tenantId: tenant.id,
          tenant: tenant.subdomain,
          role: 'agent',
          type: 'agent',
          agentName: agentName || 'Agente PDV',
          iat: Math.floor(Date.now() / 1000),
        },
        jwtSecret,
        {
          expiresIn: `${expiresInDays}d`,
          issuer: 'solis-api',
          audience: 'solis-pdv',
        }
      )

      return { token, expiresAt }
    } catch (error) {
      console.error('[AuthService] Erro ao gerar token de agente:', error)
      return null
    }
  }

  /**
   * Autentica um usuário com email e senha
   */
  static async login(
    email: string,
    password: string,
    tenantSubdomain: string,
    empresaId: string
  ): Promise<{ success: boolean; token?: string; expiresAt?: Date; error?: string; usuario?: any }> {
    try {
      // Busca o tenant
      const tenant = await prismaPublic.tenant.findUnique({
        where: { subdomain: tenantSubdomain }
      })

      if (!tenant || !tenant.active) {
        return {
          success: false,
          error: 'Tenant não encontrado ou inativo'
        }
      }

      // Busca o usuário no schema do tenant
      const tenantPrisma = await getPrismaClient(tenantSubdomain)
      const usuario = await tenantPrisma.usuario.findUnique({
        where: { email: email.toLowerCase() }
      })

      if (!usuario) {
        return {
          success: false,
          error: 'Credenciais inválidas'
        }
      }

      // Verifica se o usuário está ativo
      if (!usuario.ativo) {
        return {
          success: false,
          error: 'Usuário inativo'
        }
      }

      // Verifica a senha (usando bcrypt)
      const bcrypt = require('bcryptjs')
      const passwordMatch = await bcrypt.compare(password, usuario.passwordHash)

      if (!passwordMatch) {
        return {
          success: false,
          error: 'Credenciais inválidas'
        }
      }

      // Verifica se a empresa existe e está ativa
      const empresa = await tenantPrisma.empresa.findUnique({
        where: { id: empresaId }
      })

      if (!empresa || !empresa.ativo) {
        return {
          success: false,
          error: 'Empresa não encontrada ou inativa'
        }
      }

      // Gera o token
      const tokenData = await AuthService.generateToken(
        usuario.id,
        empresaId,
        tenant.id,
        30 // 30 dias de validade
      )

      if (!tokenData) {
        return {
          success: false,
          error: 'Erro ao gerar token de autenticação'
        }
      }

      return {
        success: true,
        token: tokenData.token,
        expiresAt: tokenData.expiresAt,
        usuario: {
          id: usuario.id,
          nome: usuario.nome,
          email: usuario.email,
          role: usuario.role
        }
      }

    } catch (error) {
      console.error('[AuthService] Erro no login:', error)
      return {
        success: false,
        error: 'Erro ao realizar login'
      }
    }
  }
}
