import { getTenant } from '@/lib/tenant'
import { validateTenant, getTenantIsolation } from '@/lib/database'

/**
 * @swagger
 * /api/health:
 *   get:
 *     tags:
 *       - Health
 *     summary: Health check da API
 *     description: Verifica o status da API e do tenant especificado, incluindo tipo de isolamento (schema ou database)
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *     responses:
 *       200:
 *         description: Status da API e tenant
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/HealthCheck'
 *       500:
 *         description: Erro interno
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Error'
 */
export async function GET() {
  try {
    const tenant = await getTenant()
    
    // Validar se o tenant existe
    const isValid = await validateTenant(tenant)
    
    // Obter informações de isolamento (schema compartilhado ou banco dedicado)
    const isolation = getTenantIsolation(tenant)
    
    return Response.json({
      tenant,
      isValid,
      isolation: {
        type: isolation.type,
        detail: isolation.type === 'DATABASE' ? isolation.database : isolation.schema,
        description: isolation.type === 'DATABASE' 
          ? '🗄️  Banco de dados dedicado (isolamento total)'
          : '📁 Schema compartilhado (custo-benefício otimizado)',
      },
      timestamp: new Date().toISOString(),
      message: isValid 
        ? `Connected to tenant: ${tenant}` 
        : `Invalid or inactive tenant: ${tenant}`
    })
  } catch (error) {
    return Response.json(
      { 
        error: 'Failed to get tenant information',
        details: error instanceof Error ? error.message : 'Unknown error'
      },
      { status: 500 }
    )
  }
}
