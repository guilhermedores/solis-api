import { NextResponse } from 'next/server'

// Endpoint raiz da API - Informações gerais
// Não documentado no Swagger (rota interna)
export async function GET() {
  return NextResponse.json({
    name: 'Solis API',
    version: '1.0.0',
    description: 'API backend do sistema Solis PDV com multi-tenancy',
    endpoints: {
      health: '/api/health?tenant={tenant}',
      produtos: '/api/produtos?tenant={tenant} (em desenvolvimento)',
      vendas: '/api/vendas?tenant={tenant} (em desenvolvimento)',
      docs: '/docs (Swagger UI)',
      openapi: '/api/docs (OpenAPI JSON)',
    },
    documentation: 'https://github.com/guilhermedores/solis',
    tenant: {
      development: 'Use query param ?tenant=demo ou header x-tenant',
      production: 'Tenant extraído do subdomínio (cliente1.solis.com.br)',
    },
  })
}
