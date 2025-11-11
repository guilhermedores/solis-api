/**
 * API de gerenciamento de Usuários
 * GET  /api/usuarios - Lista todos os usuários
 * POST /api/usuarios - Cria novo usuário
 */

import { NextRequest, NextResponse } from 'next/server'
import { UserService, CreateUsuarioInput } from '@/lib/user-service'
import { getTenant } from '@/lib/tenant'
import { requireManager } from '@/lib/auth-middleware'

/**
 * @swagger
 * /api/usuarios:
 *   get:
 *     tags:
 *       - Usuários
 *     summary: Lista todos os usuários
 *     description: Retorna lista de todos os usuários do tenant (requer autenticação manager/admin)
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *     responses:
 *       200:
 *         description: Lista de usuários
 *         content:
 *           application/json:
 *             schema:
 *               type: array
 *               items:
 *                 type: object
 *                 properties:
 *                   id:
 *                     type: string
 *                     format: uuid
 *                   nome:
 *                     type: string
 *                   email:
 *                     type: string
 *                   role:
 *                     type: string
 *                     enum: [admin, manager, operator]
 *                   ativo:
 *                     type: boolean
 *                   createdAt:
 *                     type: string
 *                     format: date-time
 *                   updatedAt:
 *                     type: string
 *                     format: date-time
 *       400:
 *         description: Tenant não identificado
 *       401:
 *         description: Não autenticado
 *       403:
 *         description: Sem permissão (requer manager ou admin)
 *       500:
 *         description: Erro interno
 */
export async function GET(request: NextRequest) {
  try {
    // Requer autenticação de manager ou admin
    const authResult = await requireManager(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    const userService = new UserService(tenant)
    const usuarios = await userService.listUsuarios()

    return NextResponse.json(usuarios)
  } catch (error) {
    console.error('Erro ao listar usuários:', error)
    return NextResponse.json(
      { error: 'Erro ao listar usuários' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/usuarios:
 *   post:
 *     tags:
 *       - Usuários
 *     summary: Cria novo usuário
 *     description: Cria um novo usuário no tenant (requer autenticação manager/admin)
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - nome
 *               - email
 *               - password
 *             properties:
 *               nome:
 *                 type: string
 *                 example: "João Silva"
 *               email:
 *                 type: string
 *                 format: email
 *                 example: "joao@empresa.com"
 *               password:
 *                 type: string
 *                 format: password
 *                 example: "senha123"
 *                 minLength: 6
 *               role:
 *                 type: string
 *                 enum: [admin, manager, operator]
 *                 default: operator
 *                 example: "operator"
 *     responses:
 *       201:
 *         description: Usuário criado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 id:
 *                   type: string
 *                   format: uuid
 *                 nome:
 *                   type: string
 *                 email:
 *                   type: string
 *                 role:
 *                   type: string
 *                 ativo:
 *                   type: boolean
 *                 createdAt:
 *                   type: string
 *                   format: date-time
 *       400:
 *         description: Dados inválidos ou email já cadastrado
 *       401:
 *         description: Não autenticado
 *       403:
 *         description: Sem permissão
 *       500:
 *         description: Erro interno
 */
export async function POST(request: NextRequest) {
  try {
    // Requer autenticação de manager ou admin
    const authResult = await requireManager(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    const body = await request.json() as CreateUsuarioInput

    // Validações básicas
    if (!body.nome || !body.email || !body.password) {
      return NextResponse.json(
        { error: 'Nome, email e senha são obrigatórios' },
        { status: 400 }
      )
    }

    if (body.password.length < 6) {
      return NextResponse.json(
        { error: 'Senha deve ter no mínimo 6 caracteres' },
        { status: 400 }
      )
    }

    // Validação de email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(body.email)) {
      return NextResponse.json(
        { error: 'Email inválido' },
        { status: 400 }
      )
    }

    // Validação de role
    const validRoles = ['admin', 'manager', 'operator']
    if (body.role && !validRoles.includes(body.role)) {
      return NextResponse.json(
        { error: 'Role inválida. Use: admin, manager ou operator' },
        { status: 400 }
      )
    }

    const userService = new UserService(tenant)
    const usuario = await userService.createUsuario(body)

    return NextResponse.json(usuario, { status: 201 })
  } catch (error: any) {
    console.error('Erro ao criar usuário:', error)

    if (error.message === 'Email já cadastrado') {
      return NextResponse.json(
        { error: error.message },
        { status: 400 }
      )
    }

    return NextResponse.json(
      { error: 'Erro ao criar usuário' },
      { status: 500 }
    )
  }
}
