/**
 * API de gerenciamento de Usuário específico
 * GET    /api/usuarios/:id - Busca usuário por ID
 * PUT    /api/usuarios/:id - Atualiza usuário
 * DELETE /api/usuarios/:id - Desativa usuário (soft delete)
 */

import { NextRequest, NextResponse } from 'next/server'
import { UserService, UpdateUsuarioInput } from '@/lib/user-service'
import { getTenant } from '@/lib/tenant'
import { requireManager, requireAuth } from '@/lib/auth-middleware'

interface RouteParams {
  params: Promise<{
    id: string
  }>
}

/**
 * @swagger
 * /api/usuarios/{id}:
 *   get:
 *     tags:
 *       - Usuários
 *     summary: Busca usuário por ID
 *     description: Retorna dados de um usuário específico (usuários podem ver apenas seus próprios dados, managers/admins veem qualquer um)
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *     responses:
 *       200:
 *         description: Dados do usuário
 *       400:
 *         description: Tenant não identificado
 *       401:
 *         description: Não autenticado
 *       403:
 *         description: Sem permissão
 *       404:
 *         description: Usuário não encontrado
 *       500:
 *         description: Erro interno
 */
export async function GET(request: NextRequest, { params }: RouteParams) {
  try {
    const { id } = await params

    // Requer autenticação
    const authResult = await requireAuth(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const { auth } = authResult

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    const userService = new UserService(tenant)
    const usuario = await userService.getUsuarioById(id)

    if (!usuario) {
      return NextResponse.json(
        { error: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    // Usuários comuns só podem ver seus próprios dados
    if (auth.role === 'operator' && auth.userId !== id) {
      return NextResponse.json(
        { error: 'Permissão negada' },
        { status: 403 }
      )
    }

    return NextResponse.json(usuario)
  } catch (error) {
    console.error('Erro ao buscar usuário:', error)
    return NextResponse.json(
      { error: 'Erro ao buscar usuário' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/usuarios/{id}:
 *   put:
 *     tags:
 *       - Usuários
 *     summary: Atualiza usuário
 *     description: Atualiza dados de um usuário (usuários podem atualizar apenas seus próprios dados, managers/admins atualizam qualquer um)
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               nome:
 *                 type: string
 *               email:
 *                 type: string
 *                 format: email
 *               password:
 *                 type: string
 *                 format: password
 *                 minLength: 6
 *               role:
 *                 type: string
 *                 enum: [admin, manager, operator]
 *               ativo:
 *                 type: boolean
 *     responses:
 *       200:
 *         description: Usuário atualizado com sucesso
 *       400:
 *         description: Dados inválidos
 *       401:
 *         description: Não autenticado
 *       403:
 *         description: Sem permissão
 *       404:
 *         description: Usuário não encontrado
 *       500:
 *         description: Erro interno
 */
export async function PUT(request: NextRequest, { params }: RouteParams) {
  try {
    const { id } = await params

    // Requer autenticação
    const authResult = await requireAuth(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const { auth } = authResult

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    const body = await request.json() as UpdateUsuarioInput

    // Usuários comuns só podem atualizar seus próprios dados
    if (auth.role === 'operator' && auth.userId !== id) {
      return NextResponse.json(
        { error: 'Permissão negada' },
        { status: 403 }
      )
    }

    // Apenas managers/admins podem alterar role e ativo
    if (auth.role === 'operator') {
      delete body.role
      delete body.ativo
    }

    // Validações
    if (body.password && body.password.length < 6) {
      return NextResponse.json(
        { error: 'Senha deve ter no mínimo 6 caracteres' },
        { status: 400 }
      )
    }

    if (body.email) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(body.email)) {
        return NextResponse.json(
          { error: 'Email inválido' },
          { status: 400 }
        )
      }
    }

    if (body.role) {
      const validRoles = ['admin', 'manager', 'operator']
      if (!validRoles.includes(body.role)) {
        return NextResponse.json(
          { error: 'Role inválida' },
          { status: 400 }
        )
      }
    }

    const userService = new UserService(tenant)
    const usuario = await userService.updateUsuario(id, body)

    return NextResponse.json(usuario)
  } catch (error: any) {
    console.error('Erro ao atualizar usuário:', error)

    if (error.message === 'Usuário não encontrado') {
      return NextResponse.json(
        { error: error.message },
        { status: 404 }
      )
    }

    if (error.message === 'Email já cadastrado') {
      return NextResponse.json(
        { error: error.message },
        { status: 400 }
      )
    }

    return NextResponse.json(
      { error: 'Erro ao atualizar usuário' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/usuarios/{id}:
 *   delete:
 *     tags:
 *       - Usuários
 *     summary: Desativa usuário (soft delete)
 *     description: Desativa um usuário (requer manager/admin)
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *     responses:
 *       200:
 *         description: Usuário desativado com sucesso
 *       400:
 *         description: Tenant não identificado
 *       401:
 *         description: Não autenticado
 *       403:
 *         description: Sem permissão
 *       404:
 *         description: Usuário não encontrado
 *       500:
 *         description: Erro interno
 */
export async function DELETE(request: NextRequest, { params }: RouteParams) {
  try {
    const { id } = await params

    // Requer autenticação de manager ou admin
    const authResult = await requireManager(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const { auth } = authResult

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    // Não permite desativar a si mesmo
    if (auth.userId === id) {
      return NextResponse.json(
        { error: 'Você não pode desativar sua própria conta' },
        { status: 400 }
      )
    }

    const userService = new UserService(tenant)
    const usuario = await userService.deactivateUsuario(id)

    return NextResponse.json({
      message: 'Usuário desativado com sucesso',
      usuario
    })
  } catch (error: any) {
    console.error('Erro ao desativar usuário:', error)

    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    return NextResponse.json(
      { error: 'Erro ao desativar usuário' },
      { status: 500 }
    )
  }
}
