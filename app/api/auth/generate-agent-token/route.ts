import { NextRequest, NextResponse } from 'next/server'
import jwt from 'jsonwebtoken'

/**
 * @swagger
 * /api/auth/generate-agent-token:
 *   post:
 *     summary: Gera token JWT para instalação do Agente PDV
 *     description: |
 *       Endpoint administrativo para gerar tokens de instalação do agente.
 *       O token contém o tenant embutido e não pode ser alterado pelo usuário.
 *       **IMPORTANTE:** Este endpoint deve ser protegido em produção.
 *     tags: [Auth]
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - tenantId
 *               - adminKey
 *             properties:
 *               tenantId:
 *                 type: string
 *                 example: demo
 *                 description: ID do tenant (cliente)
 *               adminKey:
 *                 type: string
 *                 example: your-admin-secret-key
 *                 description: Chave administrativa para autorização
 *               expiresInDays:
 *                 type: integer
 *                 default: 3650
 *                 description: Validade do token em dias (padrão 10 anos)
 *               agentName:
 *                 type: string
 *                 example: PDV Loja Centro
 *                 description: Nome identificador do agente/PDV
 *     responses:
 *       200:
 *         description: Token gerado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 token:
 *                   type: string
 *                   description: Token JWT para instalação
 *                 tenantId:
 *                   type: string
 *                   description: ID do tenant embutido no token
 *                 expiresAt:
 *                   type: string
 *                   format: date-time
 *                   description: Data de expiração do token
 *                 instructions:
 *                   type: string
 *                   description: Instruções de instalação
 *       401:
 *         description: Chave administrativa inválida
 *       400:
 *         description: Dados inválidos
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { tenantId, adminKey, expiresInDays = 3650, agentName } = body

    // Validações
    if (!tenantId || !adminKey) {
      return NextResponse.json(
        { error: 'tenantId e adminKey são obrigatórios' },
        { status: 400 }
      )
    }

    // Verifica chave administrativa (TODO: melhorar segurança em produção)
    const validAdminKey = process.env.ADMIN_SECRET_KEY || 'change-me-in-production'
    if (adminKey !== validAdminKey) {
      return NextResponse.json(
        { error: 'Chave administrativa inválida' },
        { status: 401 }
      )
    }

    // Gera token JWT com tenant embutido
    const jwtSecret = process.env.JWT_SECRET || 'your-super-secret-jwt-key-change-in-production'
    const expiresAt = new Date()
    expiresAt.setDate(expiresAt.getDate() + expiresInDays)

    const token = jwt.sign(
      {
        tenant: tenantId,
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

    return NextResponse.json({
      token,
      tenantId,
      expiresAt: expiresAt.toISOString(),
      instructions: `
=== INSTRUÇÕES DE INSTALAÇÃO ===

1. Execute o instalador do Agente PDV como Administrador
2. Quando solicitado, informe o token abaixo:

TOKEN: ${token}

3. O agente será configurado automaticamente para o tenant: ${tenantId}

IMPORTANTE:
- O token está criptografado e não pode ser alterado manualmente
- Qualquer tentativa de modificação invalidará a autenticação
- O token expira em ${expiresInDays} dias (${expiresAt.toISOString()})
- Guarde este token em local seguro para futuras instalações
      `.trim()
    })

  } catch (error) {
    console.error('[Auth] Erro ao gerar token:', error)
    return NextResponse.json(
      { error: 'Erro ao gerar token', details: error instanceof Error ? error.message : 'Erro desconhecido' },
      { status: 500 }
    )
  }
}
