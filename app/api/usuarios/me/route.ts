import { NextRequest, NextResponse } from 'next/server'
import { requireAuth } from '@/lib/auth-middleware'
import { getPrismaClient } from '@/lib/prisma'

/**
 * @swagger
 * /api/usuarios/me:
 *   get:
 *     tags:
 *       - Usuários
 *     summary: Obtém dados do usuário autenticado
 *     description: Retorna os dados do usuário atualmente logado
 *     security:
 *       - bearerAuth: []
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *     responses:
 *       200:
 *         description: Dados do usuário autenticado
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
 *                   enum: [admin, manager, user, operator]
 *                 tenantId:
 *                   type: string
 *       401:
 *         description: Não autenticado
 *       500:
 *         description: Erro interno
 */
export async function GET(request: NextRequest) {
  try {
    // Requer autenticação
    const authResult = await requireAuth(request)
    if (authResult instanceof NextResponse) {
      return authResult
    }

    const { auth } = authResult

    // Busca dados completos do usuário no banco
    const tenantPrisma = await getPrismaClient(auth.tenant)
    const usuario = await tenantPrisma.usuario.findUnique({
      where: { id: auth.userId }
    })

    if (!usuario) {
      return NextResponse.json(
        { error: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    // Retorna os dados do usuário autenticado
    return NextResponse.json({
      id: usuario.id,
      nome: usuario.nome,
      email: usuario.email,
      role: usuario.role,
      tenantId: auth.tenantId,
    })
  } catch (error) {
    console.error('Erro ao obter dados do usuário:', error)
    return NextResponse.json(
      { error: 'Erro ao obter dados do usuário' },
      { status: 500 }
    )
  }
}
