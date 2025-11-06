/**
 * API de gerenciamento de Empresa específica
 * GET    /api/empresas/:id          - Busca empresa por ID
 * PUT    /api/empresas/:id          - Atualiza empresa
 * DELETE /api/empresas/:id          - Deleta empresa
 */

import { NextRequest, NextResponse } from 'next/server'
import { EmpresaService, UpdateEmpresaInput } from '@/lib/empresa-service'
import { getTenant } from '@/lib/tenant'

interface RouteParams {
  params: {
    id: string
  }
}

/**
 * @swagger
 * /api/empresas/{id}:
 *   get:
 *     tags:
 *       - Empresas
 *     summary: Busca empresa por ID
 *     description: Retorna detalhes de uma empresa específica
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
 *         description: Empresa encontrada
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Empresa'
 *       400:
 *         description: Tenant não identificado
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
    const empresa = await empresaService.getEmpresaById(params.id)
    
    if (!empresa) {
      return NextResponse.json(
        { error: 'Empresa não encontrada' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(empresa)
  } catch (error) {
    console.error('Erro ao buscar empresa:', error)
    return NextResponse.json(
      { error: 'Erro ao buscar empresa' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/empresas/{id}:
 *   put:
 *     tags:
 *       - Empresas
 *     summary: Atualiza empresa
 *     description: Atualiza dados de uma empresa existente
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *         description: ID da empresa
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               razaoSocial:
 *                 type: string
 *               nomeFantasia:
 *                 type: string
 *               inscricaoEstadual:
 *                 type: string
 *               inscricaoMunicipal:
 *                 type: string
 *               logradouro:
 *                 type: string
 *               numero:
 *                 type: string
 *               complemento:
 *                 type: string
 *               bairro:
 *                 type: string
 *               cidade:
 *                 type: string
 *               uf:
 *                 type: string
 *               cep:
 *                 type: string
 *               telefone:
 *                 type: string
 *               email:
 *                 type: string
 *               site:
 *                 type: string
 *               regimeTributario:
 *                 type: string
 *                 enum: [simples_nacional, lucro_presumido, lucro_real]
 *               certificadoDigital:
 *                 type: string
 *               senhaCertificado:
 *                 type: string
 *               ambienteFiscal:
 *                 type: string
 *                 enum: [producao, homologacao]
 *               logo:
 *                 type: string
 *               ativo:
 *                 type: boolean
 *     responses:
 *       200:
 *         description: Empresa atualizada
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Empresa'
 *       400:
 *         description: Dados inválidos ou tenant não identificado
 *       404:
 *         description: Empresa não encontrada
 *       500:
 *         description: Erro interno
 */
export async function PUT(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }
    
    const empresaService = new EmpresaService(tenant)
    const body = await request.json() as UpdateEmpresaInput
    
    const empresa = await empresaService.updateEmpresa(params.id, body)
    
    return NextResponse.json(empresa)
  } catch (error: any) {
    console.error('Erro ao atualizar empresa:', error)
    
    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Empresa não encontrada' },
        { status: 404 }
      )
    }
    
    if (error.message.includes('inválido')) {
      return NextResponse.json(
        { error: error.message },
        { status: 400 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao atualizar empresa' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/empresas/{id}:
 *   delete:
 *     tags:
 *       - Empresas
 *     summary: Desativa empresa
 *     description: Desativa uma empresa (soft delete)
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
 *         description: Empresa desativada com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 message:
 *                   type: string
 *                   example: Empresa desativada com sucesso
 *       400:
 *         description: Tenant não identificado
 *       404:
 *         description: Empresa não encontrada
 *       500:
 *         description: Erro interno
 */
export async function DELETE(request: NextRequest, { params }: RouteParams) {
  try {
    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }
    
    const empresaService = new EmpresaService(tenant)
    await empresaService.deactivateEmpresa(params.id)
    
    return NextResponse.json({
      message: 'Empresa desativada com sucesso',
    })
  } catch (error: any) {
    console.error('Erro ao desativar empresa:', error)
    
    if (error.code === 'P2025') {
      return NextResponse.json(
        { error: 'Empresa não encontrada' },
        { status: 404 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao desativar empresa' },
      { status: 500 }
    )
  }
}
