import { NextRequest, NextResponse } from 'next/server'
import { prismaPublic } from '@/lib/prisma'
import { AuthService } from '@/lib/auth-service'

/**
 * @swagger
 * /api/auth/generate-agent-token:
 *   post:
 *     summary: Gera token JWT para instalação do Agente PDV
 *     description: |
 *       Endpoint administrativo para gerar tokens de instalação do agente.
 *       O token contém o empresaId e tenantId embutidos e não podem ser alterados pelo usuário.
 *       **IMPORTANTE:** Este endpoint deve ser protegido em produção.
 *     tags: [Auth]
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - empresaId
 *               - tenantId
 *               - adminKey
 *             properties:
 *               empresaId:
 *                 type: string
 *                 format: uuid
 *                 example: 123e4567-e89b-12d3-a456-426614174000
 *                 description: ID da empresa
 *               tenantId:
 *                 type: string
 *                 example: demo
 *                 description: Subdomain/ID do tenant
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
 *                 empresaId:
 *                   type: string
 *                   description: ID da empresa embutido no token
 *                 tenantId:
 *                   type: string
 *                   description: ID do tenant embutido no token
 *                 empresaNome:
 *                   type: string
 *                   description: Nome da empresa
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
 *       404:
 *         description: Empresa ou tenant não encontrado
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    const { empresaId, tenantId, adminKey, expiresInDays = 3650, agentName } = body

    // Validações
    if (!empresaId || !tenantId || !adminKey) {
      return NextResponse.json(
        { error: 'empresaId, tenantId e adminKey são obrigatórios' },
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

    // Valida se o tenant existe e está ativo
    const tenant = await prismaPublic.tenant.findUnique({
      where: { subdomain: tenantId }
    })

    if (!tenant) {
      return NextResponse.json(
        { error: 'Tenant não encontrado' },
        { status: 404 }
      )
    }

    if (!tenant.active) {
      return NextResponse.json(
        { error: 'Tenant está inativo' },
        { status: 400 }
      )
    }

    // Gera token de agente usando AuthService
    const tokenData = await AuthService.generateAgentToken(
      empresaId,
      tenant.id,
      expiresInDays,
      agentName
    )

    if (!tokenData) {
      return NextResponse.json(
        { error: 'Erro ao gerar token. Verifique se a empresa existe e está ativa.' },
        { status: 400 }
      )
    }

    // Busca dados da empresa para exibir nas instruções
    const { getPrismaClient } = await import('@/lib/prisma')
    const tenantPrisma = await getPrismaClient(tenantId)
    const empresa = await tenantPrisma.empresa.findUnique({
      where: { id: empresaId },
      select: {
        id: true,
        razaoSocial: true,
        nomeFantasia: true,
        cnpj: true
      }
    })

    if (!empresa) {
      return NextResponse.json(
        { error: 'Empresa não encontrada' },
        { status: 404 }
      )
    }

    return NextResponse.json({
      token: tokenData.token,
      empresaId: empresa.id,
      tenantId: tenant.id,
      tenant: tenant.subdomain,
      empresaNome: empresa.nomeFantasia || empresa.razaoSocial,
      cnpj: empresa.cnpj,
      expiresAt: tokenData.expiresAt.toISOString(),
      instructions: `
=== INSTRUÇÕES DE INSTALAÇÃO ===

1. Execute o instalador do Agente PDV como Administrador
2. Quando solicitado, informe o token abaixo:

TOKEN: ${tokenData.token}

3. O agente será configurado automaticamente para:
   - Empresa: ${empresa.nomeFantasia || empresa.razaoSocial}
   - CNPJ: ${empresa.cnpj}
   - Tenant: ${tenant.subdomain}

IMPORTANTE:
- O token está criptografado e não pode ser alterado manualmente
- Qualquer tentativa de modificação invalidará a autenticação
- O token expira em ${expiresInDays} dias (${tokenData.expiresAt.toISOString()})
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
