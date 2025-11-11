/**
 * Middleware de autenticação JWT
 * Valida tokens e anexa informações do usuário autenticado ao contexto
 */

import { NextRequest, NextResponse } from 'next/server'
import { AuthService, ValidateTokenResult } from './auth-service'

export interface AuthenticatedContext {
  userId: string
  tenantId: string
  tenant: string
  role: string
  usuario: {
    id: string
    nome: string
    email: string
    role: string
  }
}

/**
 * Extrai o token do header Authorization
 */
function extractToken(request: NextRequest): string | null {
  const authHeader = request.headers.get('authorization')
  
  if (!authHeader) {
    return null
  }

  // Suporta formato: "Bearer <token>"
  const parts = authHeader.split(' ')
  if (parts.length === 2 && parts[0] === 'Bearer') {
    return parts[1]
  }

  // Suporta formato: "<token>" direto
  return authHeader
}

/**
 * Valida o token JWT e retorna o contexto autenticado
 * Retorna null se o token for inválido
 */
export async function validateAuth(request: NextRequest): Promise<AuthenticatedContext | null> {
  const token = extractToken(request)

  if (!token) {
    return null
  }

  const validation = await AuthService.validateToken(token)

  if (!validation.valid || !validation.payload || !validation.usuario) {
    return null
  }

  return {
    userId: validation.payload.userId,
    tenantId: validation.payload.tenantId,
    tenant: validation.payload.tenant,
    role: validation.payload.role,
    usuario: validation.usuario
  }
}

/**
 * Middleware que exige autenticação
 * Uso: const auth = await requireAuth(request)
 */
export async function requireAuth(
  request: NextRequest
): Promise<{ auth: AuthenticatedContext } | NextResponse> {
  const auth = await validateAuth(request)

  if (!auth) {
    return NextResponse.json(
      { error: 'Autenticação necessária' },
      { status: 401 }
    )
  }

  return { auth }
}

/**
 * Middleware que exige autenticação e valida role específica
 */
export async function requireRole(
  request: NextRequest,
  allowedRoles: string[]
): Promise<{ auth: AuthenticatedContext } | NextResponse> {
  const authResult = await requireAuth(request)

  // Se já retornou erro de autenticação, retorna
  if (authResult instanceof NextResponse) {
    return authResult
  }

  const { auth } = authResult

  // Valida se o usuário tem uma das roles permitidas
  if (!allowedRoles.includes(auth.role)) {
    return NextResponse.json(
      { error: 'Permissão negada' },
      { status: 403 }
    )
  }

  return { auth }
}

/**
 * Middleware que exige autenticação de admin
 */
export async function requireAdmin(
  request: NextRequest
): Promise<{ auth: AuthenticatedContext } | NextResponse> {
  return requireRole(request, ['admin'])
}

/**
 * Middleware que exige autenticação de manager ou admin
 */
export async function requireManager(
  request: NextRequest
): Promise<{ auth: AuthenticatedContext } | NextResponse> {
  return requireRole(request, ['admin', 'manager'])
}
