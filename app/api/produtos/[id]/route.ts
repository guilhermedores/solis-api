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
  preco_custo?: number | null
}

/**
 * @swagger
 * /api/produtos/{id}:
 *   get:
 *     tags:
 *       - Produtos
 *     summary: Buscar produto por ID
 *     description: Retorna um produto específico com preço
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *         description: ID do produto
 *     responses:
 *       200:
 *         description: Produto encontrado
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Produto'
 *       404:
 *         description: Produto não encontrado
 *       500:
 *         description: Erro interno
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const tenant = await getTenant()
    const { id } = await params

    // Obtém cliente Prisma configurado para o tenant
    const prisma = await getPrismaClient(tenant)
    
    const produto = await prisma.produto.findUnique({
      where: { id },
      include: {
        precos: {
          where: { ativo: true },
          orderBy: { createdAt: 'desc' },
          take: 1
        }
      }
    })
    
    if (!produto) {
      return NextResponse.json(
        { error: 'Produto não encontrado' },
        { status: 404 }
      )
    }

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
      preco_venda: produto.precos[0]?.precoVenda || null,
      preco_custo: produto.precos[0]?.precoCusto || null
    }
    
    return NextResponse.json(produtoFormatado)
  } catch (error) {
    console.error('[Produtos] Erro ao buscar:', error)
    return NextResponse.json(
      { 
        error: 'Erro ao buscar produto',
        details: error instanceof Error ? error.message : 'Erro desconhecido'
      },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/produtos/{id}:
 *   put:
 *     tags:
 *       - Produtos
 *     summary: Atualizar produto
 *     description: Atualiza um produto existente e seu preço
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *         description: ID do produto
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             properties:
 *               codigo_barras:
 *                 type: string
 *               codigo_interno:
 *                 type: string
 *               nome:
 *                 type: string
 *               descricao:
 *                 type: string
 *               ncm:
 *                 type: string
 *               cest:
 *                 type: string
 *               unidade_medida:
 *                 type: string
 *               preco_venda:
 *                 type: number
 *               preco_custo:
 *                 type: number
 *               ativo:
 *                 type: boolean
 *     responses:
 *       200:
 *         description: Produto atualizado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/Produto'
 *       404:
 *         description: Produto não encontrado
 *       500:
 *         description: Erro interno
 */
export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const tenant = await getTenant()
    const { id } = await params
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
      ativo
    } = body

    // Obtém cliente Prisma configurado para o tenant
    const prisma = await getPrismaClient(tenant)
    
    // Verificar se produto existe
    const existing = await prisma.produto.findUnique({
      where: { id }
    })
    
    if (!existing) {
      return NextResponse.json(
        { error: 'Produto não encontrado' },
        { status: 404 }
      )
    }
    
    // Preparar dados para atualização (apenas campos fornecidos)
    const updateData: any = {}
    if (codigo_barras !== undefined) updateData.codigoBarras = codigo_barras
    if (codigo_interno !== undefined) updateData.codigoInterno = codigo_interno
    if (nome !== undefined) updateData.nome = nome
    if (descricao !== undefined) updateData.descricao = descricao
    if (ncm !== undefined) updateData.ncm = ncm
    if (cest !== undefined) updateData.cest = cest
    if (unidade_medida !== undefined) updateData.unidadeMedida = unidade_medida
    if (ativo !== undefined) updateData.ativo = ativo
    
    // Atualizar produto
    const produto = await prisma.produto.update({
      where: { id },
      data: updateData,
      include: {
        precos: {
          where: { ativo: true },
          orderBy: { createdAt: 'desc' },
          take: 1
        }
      }
    })
    
    // Atualizar preço se fornecido
    if (preco_venda !== undefined) {
      // Desativar preços antigos
      await prisma.produtoPreco.updateMany({
        where: { produtoId: id },
        data: { ativo: false }
      })
      
      // Inserir novo preço
      await prisma.produtoPreco.create({
        data: {
          produtoId: id,
          precoVenda: preco_venda,
          precoCusto: preco_custo || null,
          ativo: true
        }
      })

      // Buscar produto atualizado com novo preço
      const produtoAtualizado = await prisma.produto.findUnique({
        where: { id },
        include: {
          precos: {
            where: { ativo: true },
            orderBy: { createdAt: 'desc' },
            take: 1
          }
        }
      })

      // Transformar para o formato esperado pela API (snake_case)
      const produtoFormatado = {
        id: produtoAtualizado!.id,
        codigo_barras: produtoAtualizado!.codigoBarras,
        codigo_interno: produtoAtualizado!.codigoInterno,
        nome: produtoAtualizado!.nome,
        descricao: produtoAtualizado!.descricao,
        unidade_medida: produtoAtualizado!.unidadeMedida,
        categoria: produtoAtualizado!.categoria,
        ncm: produtoAtualizado!.ncm,
        cest: produtoAtualizado!.cest,
        cfop: produtoAtualizado!.cfop,
        ativo: produtoAtualizado!.ativo,
        created_at: produtoAtualizado!.createdAt,
        updated_at: produtoAtualizado!.updatedAt,
        preco_venda: produtoAtualizado!.precos[0]?.precoVenda || null,
        preco_custo: produtoAtualizado!.precos[0]?.precoCusto || null
      }

      return NextResponse.json(produtoFormatado)
    }

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
      preco_venda: produto.precos[0]?.precoVenda || null,
      preco_custo: produto.precos[0]?.precoCusto || null
    }
    
    return NextResponse.json(produtoFormatado)
  } catch (error) {
    console.error('[Produtos] Erro ao atualizar:', error)
    return NextResponse.json(
      { 
        error: 'Erro ao atualizar produto',
        details: error instanceof Error ? error.message : 'Erro desconhecido'
      },
      { status: 500 }
    )
  }
}

/**
 * @swagger
 * /api/produtos/{id}:
 *   delete:
 *     tags:
 *       - Produtos
 *     summary: Deletar produto
 *     description: Remove um produto (soft delete - marca como inativo)
 *     parameters:
 *       - $ref: '#/components/parameters/tenantQuery'
 *       - $ref: '#/components/parameters/tenantHeader'
 *       - name: id
 *         in: path
 *         required: true
 *         schema:
 *           type: string
 *           format: uuid
 *         description: ID do produto
 *     responses:
 *       200:
 *         description: Produto deletado com sucesso
 *         content:
 *           application/json:
 *             schema:
 *               type: object
 *               properties:
 *                 message:
 *                   type: string
 *                   example: Produto deletado com sucesso
 *       404:
 *         description: Produto não encontrado
 *       500:
 *         description: Erro interno
 */
export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const tenant = await getTenant()
    const { id } = await params

    // Obtém cliente Prisma configurado para o tenant
    const prisma = await getPrismaClient(tenant)
    
    // Verificar se produto existe
    const existing = await prisma.produto.findUnique({
      where: { id }
    })
    
    if (!existing) {
      return NextResponse.json(
        { error: 'Produto não encontrado' },
        { status: 404 }
      )
    }
    
    // Soft delete - marca como inativo
    await prisma.produto.update({
      where: { id },
      data: { ativo: false }
    })
    
    return NextResponse.json({
      message: 'Produto deletado com sucesso'
    })
  } catch (error) {
    console.error('[Produtos] Erro ao deletar:', error)
    return NextResponse.json(
      { 
        error: 'Erro ao deletar produto',
        details: error instanceof Error ? error.message : 'Erro desconhecido'
      },
      { status: 500 }
    )
  }
}
