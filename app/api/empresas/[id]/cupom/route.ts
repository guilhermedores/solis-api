/**
 * API para obter dados da empresa para cupom fiscal
 * GET /api/empresas/:id/cupom - Retorna dados sem informações sensíveis
 */

import { NextRequest, NextResponse } from 'next/server'
import { EmpresaService } from '@/lib/empresa-service'
import { getTenant } from '@/lib/tenant'

interface RouteParams {
  params: {
    id: string
  }
}

/**
 * @swagger
 * /api/empresas/{id}/cupom:
 *   get:
 *     tags:
 *       - Empresas
 *     summary: Obtém dados da empresa para cupom fiscal
 *     description: Retorna dados da empresa para emissão de cupom (sem informações sensíveis como certificado e senha)
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *         description: ID da empresa
 *     responses:
 *       200:
 *         description: Dados da empresa para cupom
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 id:
 *                   type: string
 *                   format: uuid
 *                 razaoSocial:
 *                   type: string
 *                 nomeFantasia:
 *                   type: string
 *                 cnpj:
 *                   type: string
 *                 inscricaoEstadual:
 *                   type: string
 *                 inscricaoMunicipal:
 *                   type: string
 *                 logradouro:
 *                   type: string
 *                 numero:
 *                   type: string
 *                 complemento:
 *                   type: string
 *                 bairro:
 *                   type: string
 *                 cidade:
 *                   type: string
 *                 uf:
 *                   type: string
 *                 cep:
 *                   type: string
 *                 telefone:
 *                   type: string
 *                 email:
 *                   type: string
 *                 site:
 *                   type: string
 *                 regimeTributario:
 *                   type: string
 *                 ambienteFiscal:
 *                   type: string
 *                 logo:
 *                   type: string
 *                 ativo:
 *                   type: boolean
 *       400:
 *         description: Tenant não identificado ou empresa inativa
 *       404:
 *         description: Empresa não encontrada
 *       500:
 *         description: Erro interno
 */
export async function GET(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }
    
    const empresaService = new EmpresaService(tenant)
    const empresa = await empresaService.getEmpresaForCupom(params.id)
    
    if (!empresa) {
      return NextResponse.json(
        { error: 'Empresa não encontrada' },
        { status: 404 }
      )
    }
    
    if (!empresa.ativo) {
      return NextResponse.json(
        { error: 'Empresa está inativa' },
        { status: 400 }
      )
    }
    
    return NextResponse.json(empresa)
  } catch (error) {
    console.error('Erro ao buscar dados da empresa:', error)
    return NextResponse.json(
      { error: 'Erro ao buscar dados da empresa' },
      { status: 500 }
    )
  }
}
