import { NextResponse } from 'next/server'
import { swaggerSpec } from '@/lib/swagger'

// Endpoint que retorna a especificação OpenAPI em JSON
// Não documentado no Swagger (rota interna de metadados)
export async function GET() {
  return NextResponse.json(swaggerSpec)
}
