/**
 * API de gerenciamento de Empresas
 * GET    /api/empresas       - Lista todas as empresas
 * GET    /api/empresas/:id   - Busca empresa por ID
 * POST   /api/empresas       - Cria nova empresa
 * PUT    /api/empresas/:id   - Atualiza empresa
 * DELETE /api/empresas/:id   - Deleta empresa
 */

import { NextRequest, NextResponse } from 'next/server'
import { EmpresaService, CreateEmpresaInput } from '@/lib/empresa-service'
import { getTenant } from '@/lib/tenant'

/**
 * @swagger
 * /api/empresas:
 *   get:
 *     tags:
 *       - Empresas
 *     summary: Lista todas as empresas
 *     description: Retorna lista paginada de empresas do tenant
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: page
 *         in: query
 *         schema:
 *           type: integer
 *           default: 1
 *         description: Número da página
 *       - name: limit
 *         in: query
 *         schema:
 *           type: integer
 *           default: 50
 *         description: Itens por página
 *       - name: ativo
 *         in: query
 *         schema:
 *           type: boolean
 *         description: Filtrar por empresas ativas
 *     responses:
 *       200:
 *         description: Lista de empresas
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 empresas:
 *                   type: array
 *                   items:
 *                     $ref: '#/components/schemas/Empresa'
 *                 total:
 *                   type: integer
 *                 page:
 *                   type: integer
 *                 limit:
 *                   type: integer
 *       400:
 *         description: Tenant não identificado
 *       500:
 *         description: Erro interno
 */
export async function GET(request: NextRequest) {
  try {
    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }
    
    const empresaService = new EmpresaService(tenant)
    const searchParams = request.nextUrl.searchParams
    
    // Paginação
    const page = parseInt(searchParams.get('page') || '1')
    const limit = parseInt(searchParams.get('limit') || '50')
    
    // Filtro por ativo
    const ativoParam = searchParams.get('ativo')
    const ativo = ativoParam !== null ? ativoParam === 'true' : undefined
    
    const result = await empresaService.listEmpresas(page, limit, ativo)
    
    return NextResponse.json(result)
  } catch (error) {
    console.error('Erro ao listar empresas:', error)
    return NextResponse.json(
      { error: 'Erro ao listar empresas' },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/empresas:
 *   post:
 *     tags:
 *       - Empresas
 *     summary: Cria nova empresa
 *     description: Cadastra uma nova empresa para o tenant
 *     parameters:
 *       - $ref: '#/components/parameters/tenantHeader'
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - razaoSocial
 *               - cnpj
 *               - logradouro
 *               - numero
 *               - bairro
 *               - cidade
 *               - uf
 *               - cep
 *               - regimeTributario
 *             properties:
 *               razaoSocial:
 *                 type: string
 *                 example: EMPRESA DEMO LTDA
 *               nomeFantasia:
 *                 type: string
 *                 example: Loja Demo
 *               cnpj:
 *                 type: string
 *                 example: "12345678000190"
 *               inscricaoEstadual:
 *                 type: string
 *               inscricaoMunicipal:
 *                 type: string
 *               logradouro:
 *                 type: string
 *                 example: Rua das Flores
 *               numero:
 *                 type: string
 *                 example: "123"
 *               complemento:
 *                 type: string
 *               bairro:
 *                 type: string
 *                 example: Centro
 *               cidade:
 *                 type: string
 *                 example: São Paulo
 *               uf:
 *                 type: string
 *                 example: SP
 *               cep:
 *                 type: string
 *                 example: "01234567"
 *               telefone:
 *                 type: string
 *               email:
 *                 type: string
 *                 format: email
 *               site:
 *                 type: string
 *               regimeTributario:
 *                 type: string
 *                 enum: [simples_nacional, lucro_presumido, lucro_real]
 *               certificadoDigital:
 *                 type: string
 *                 description: Certificado digital em Base64
 *               senhaCertificado:
 *                 type: string
 *                 description: Senha do certificado (será criptografada)
 *               ambienteFiscal:
 *                 type: string
 *                 enum: [producao, homologacao]
 *                 default: homologacao
 *               logo:
 *                 type: string
 *                 description: Logo em Base64
 *     responses:
 *       201:
 *         description: Empresa criada com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Empresa'
 *       400:
 *         description: Dados inválidos ou tenant não identificado
 *       500:
 *         description: Erro interno
 */
export async function POST(request: NextRequest) {
  try {
    const tenant = await getTenant()
    if (!tenant) {
      return NextResponse.json({ error: 'Tenant não identificado' }, { status: 400 })
    }
    
    const empresaService = new EmpresaService(tenant)
    const body = await request.json() as CreateEmpresaInput
    
    // Validações básicas
    if (!body.razaoSocial || !body.cnpj || !body.logradouro || !body.numero || 
        !body.bairro || !body.cidade || !body.uf || !body.cep) {
      return NextResponse.json(
        { error: 'Campos obrigatórios: razaoSocial, cnpj, logradouro, numero, bairro, cidade, uf, cep' },
        { status: 400 }
      )
    }
    
    const empresa = await empresaService.createEmpresa(body)
    
    return NextResponse.json(empresa, { status: 201 })
  } catch (error: any) {
    console.error('Erro ao criar empresa:', error)
    
    // Erros de validação
    if (error.message.includes('inválido') || error.message.includes('unique')) {
      return NextResponse.json(
        { error: error.message },
        { status: 400 }
      )
    }
    
    return NextResponse.json(
      { error: 'Erro ao criar empresa' },
      { status: 500 }
    )
  }
}
