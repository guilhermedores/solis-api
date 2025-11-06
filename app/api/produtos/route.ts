import { NextRequest, NextResponse } from 'next/server'
import { getTenant } from '@/lib/tenant'
import { getPrismaClient } from '@/lib/prisma'

interface Produto {
  id: string
  codigo_barras: string | null
  codigo_interno: string | null
  nome: string
  descricao: string | null
  ncm: string | null
  cest: string | null
  unidade_medida: string
  ativo: boolean
  created_at: string
  updated_at: string
  preco_venda?: number | null
}

/**
 * @swagger
 * /api/produtos:
 *   get:
 *     tags:
 *       - Produtos
 *     summary: Listar produtos
 *     description: Retorna a lista de produtos do tenant com preços
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: search
 *         in: query
 *         schema:
 *           type: string
 *         description: Buscar por nome, código de barras ou código interno
 *       - name: ativo
 *         in: query
 *         schema:
 *           type: boolean
 *         description: Filtrar por produtos ativos/inativos
 *       - name: limit
 *         in: query
 *         schema:
 *           type: integer
 *           default: 50
 *         description: Limite de resultados
 *       - name: offset
 *         in: query
 *         schema:
 *           type: integer
 *           default: 0
 *         description: Offset para paginação
 *     responses:
 *       200:
 *         description: Lista de produtos
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 produtos:
 *                   type: array
 *                   items:
 *                     $ref: '#/components/schemas/Produto'
 *                 total:
 *                   type: integer
 *                 limit:
 *                   type: integer
 *                 offset:
 *                   type: integer
 *       500:
 *         description: Erro interno
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Error'
 */
export async function GET(request: NextRequest) {
  try {
    const tenant = await getTenant()
    const { searchParams } = new URL(request.url)
    
    const search = searchParams.get('search')
    const ativo = searchParams.get('ativo')
    const limit = parseInt(searchParams.get('limit') || '50')
    const offset = parseInt(searchParams.get('offset') || '0')

    // Obtém cliente Prisma configurado para o tenant
    const prisma = await getPrismaClient(tenant)

    // Construir filtros dinamicamente
    const where: any = {}
    
    if (ativo !== null && ativo !== undefined) {
      where.ativo = ativo === 'true'
    }

    if (search) {
      where.OR = [
        { nome: { contains: search, mode: 'insensitive' } },
        { codigoBarras: { contains: search, mode: 'insensitive' } },
        { codigoInterno: { contains: search, mode: 'insensitive' } }
      ]
    }

    // Buscar produtos com Prisma (com relacionamento precos)
    const [produtos, total] = await Promise.all([
      prisma.produto.findMany({
        where,
        include: {
          precos: {
            where: { ativo: true },
            orderBy: { createdAt: 'desc' },
            take: 1
          }
        },
        orderBy: { nome: 'asc' },
        skip: offset,
        take: limit
      }),
      prisma.produto.count({ where })
    ])

    // Transformar dados para o formato esperado pela API (snake_case)
    const produtosFormatados = produtos.map((produto: any) => ({
      id: produto.id,
      codigo_barras: produto.codigoBarras,
      codigo_interno: produto.codigoInterno,
      nome: produto.nome,
      descricao: produto.descricao,
      unidade_medida: produto.unidadeMedida,
      categoria: produto.categoria,
      ncm: produto.ncm,
      cest: produto.cest,
      cfop: produto.cfop,
      ativo: produto.ativo,
      created_at: produto.createdAt,
      updated_at: produto.updatedAt,
      preco_venda: produto.precos[0]?.precoVenda || null,
      preco_custo: produto.precos[0]?.precoCusto || null
    }))
    
    return NextResponse.json({
      produtos: produtosFormatados,
      total,
      limit,
      offset
    })
  } catch (error) {
    console.error('[Produtos] Erro ao listar:', error)
    return NextResponse.json(
      { 
        error: 'Erro ao listar produtos',
        details: error instanceof Error ? error.message : 'Erro desconhecido'
      },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/produtos:
 *   post:
 *     tags:
 *       - Produtos
 *     summary: Criar produto
 *     description: Cria um novo produto com preço
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required:
 *               - nome
 *               - unidade_medida
 *               - preco_venda
 *             properties:
 *               codigo_barras:
 *                 type: string
 *                 example: "7891000100103"
 *               codigo_interno:
 *                 type: string
 *                 example: "PROD001"
 *               nome:
 *                 type: string
 *                 example: "COCA-COLA 2L"
 *               descricao:
 *                 type: string
 *                 example: "Refrigerante Coca-Cola 2 Litros"
 *               ncm:
 *                 type: string
 *               cest:
 *                 type: string
 *               unidade_medida:
 *                 type: string
 *                 example: "UN"
 *               preco_venda:
 *                 type: number
 *                 example: 8.90
 *               preco_custo:
 *                 type: number
 *                 example: 5.50
 *               ativo:
 *                 type: boolean
 *                 default: true
 *     responses:
 *       201:
 *         description: Produto criado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Produto'
 *       400:
 *         description: Dados inválidos
 *       500:
 *         description: Erro interno
 */
export async function POST(request: NextRequest) {
  try {
    const tenant = await getTenant()
    const body = await request.json()
    
    const {
      codigo_barras,
      codigo_interno,
      nome,
      descricao,
      ncm,
      cest,
      unidade_medida,
      preco_venda,
      preco_custo,
      ativo = true
    } = body
    
    // Validações
    if (!nome || !unidade_medida) {
      return NextResponse.json(
        { error: 'Nome e unidade de medida são obrigatórios' },
        { status: 400 }
      )
    }
    
    if (!preco_venda || preco_venda <= 0) {
      return NextResponse.json(
        { error: 'Preço de venda é obrigatório e deve ser maior que zero' },
        { status: 400 }
      )
    }

    // Obtém cliente Prisma configurado para o tenant
    const prisma = await getPrismaClient(tenant)

    // Criar produto com preço em uma transação
    const produto = await prisma.produto.create({
      data: {
        codigoBarras: codigo_barras,
        codigoInterno: codigo_interno,
        nome,
        descricao,
        unidadeMedida: unidade_medida,
        ncm,
        cest,
        ativo,
        precos: {
          create: {
            precoVenda: preco_venda,
            precoCusto: preco_custo || null
          }
        }
      },
      include: {
        precos: {
          where: { ativo: true }
        }
      }
    })

    // Transformar para o formato esperado pela API (snake_case)
    const produtoFormatado = {
      id: produto.id,
      codigo_barras: produto.codigoBarras,
      codigo_interno: produto.codigoInterno,
      nome: produto.nome,
      descricao: produto.descricao,
      unidade_medida: produto.unidadeMedida,
      categoria: produto.categoria,
      ncm: produto.ncm,
      cest: produto.cest,
      cfop: produto.cfop,
      ativo: produto.ativo,
      created_at: produto.createdAt,
      updated_at: produto.updatedAt,
      preco_venda: produto.precos[0].precoVenda,
      preco_custo: produto.precos[0].precoCusto
    }
    
    return NextResponse.json(produtoFormatado, { status: 201 })
  } catch (error) {
    console.error('[Produtos] Erro ao criar:', error)
    return NextResponse.json(
      { 
        error: 'Erro ao criar produto',
        details: error instanceof Error ? error.message : 'Erro desconhecido'
      },
      { status: 500 }
    )
  }
}
