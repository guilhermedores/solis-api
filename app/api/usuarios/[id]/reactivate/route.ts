/**
 * API de reativação de Usuário
 * POST /api/usuarios/:id/reactivate - Reativa usuário desativado
 */

import { NextRequest, NextResponse } from 'next/server'
import { UsuarioService } from '@/lib/usuario-service'
import { getTenant } from '@/lib/tenant'
import { requireManager } from '@/lib/auth-middleware'

interface RouteParams {
  params: Promise<{
    id: string
  }>
}

/**
 * @swagger
 * /api/usuarios/{id}/reactivate:
 *   post:
 *     tags:
 *       - Usuários
 *     summary: Reativa usuário desativado
 *     description: Reativa um usuário que foi desativado (requer manager/admin)
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
 *         description: Usuário reativado com sucesso
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
export async function POST(request: NextRequest, { params }: RouteParams) {
  try {
    const { id } = await params

    // Requer autenticação de manager ou admin
    const authResult = await requireManager(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }

    const usuarioService = new UsuarioService(tenant)
    const usuario = await usuarioService.reactivateUsuario(id)

    return NextResponse.json({
      message: 'Usuário reativado com sucesso',
      usuario
    })
  } catch (error: any) {
    console.error('Erro ao reativar usuário:', error)

    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    return NextResponse.json(
      { error: 'Erro ao reativar usuário' },
      { status: 500 }
    )
  }
}
