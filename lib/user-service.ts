/**
 * Serviço para gerenciamento de usuários
 */

import { getPrismaClient } from './prisma'
import bcrypt from 'bcryptjs'

export interface CreateUsuarioInput {
  nome: string
  email: string
  password: string
  role?: string
}

export interface UpdateUsuarioInput {
  nome?: string
  email?: string
  password?: string
  role?: string
  ativo?: boolean
}

export class UserService {
  private prismaPromise: Promise<ReturnType<typeof getPrismaClient> extends Promise<infer T> ? T : never>
  private tenant: string

  constructor(tenant: string) {
    this.tenant = tenant
    this.prismaPromise = getPrismaClient(tenant)
  }

  /**
   * Lista todos os usuários
   */
  async listUsuarios() {
    const prisma = await this.prismaPromise
    return prisma.usuario.findMany({
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
        // Não retorna passwordHash
      },
      orderBy: {
        nome: 'asc'
      }
    })
  }

  /**
   * Busca usuário por ID
   */
  async getUsuarioById(id: string) {
    const prisma = await this.prismaPromise
    return prisma.usuario.findUnique({
      where: { id },
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
        // Não retorna passwordHash
      }
    })
  }

  /**
   * Busca usuário por email
   */
  async getUsuarioByEmail(email: string) {
    const prisma = await this.prismaPromise
    return prisma.usuario.findUnique({
      where: { email: email.toLowerCase() },
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
        // Não retorna passwordHash
      }
    })
  }

  /**
   * Cria um novo usuário
   */
  async createUsuario(data: CreateUsuarioInput) {
    const prisma = await this.prismaPromise
    
    // Valida se o email já existe
    const existing = await prisma.usuario.findUnique({
      where: { email: data.email.toLowerCase() }
    })

    if (existing) {
      throw new Error('Email já cadastrado')
    }

    // Hash da senha
    const salt = await bcrypt.genSalt(10)
    const passwordHash = await bcrypt.hash(data.password, salt)

    // Cria o usuário
    const usuario = await prisma.usuario.create({
      data: {
        nome: data.nome,
        email: data.email.toLowerCase(),
        passwordHash,
        role: data.role || 'operator'
      },
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
      }
    })

    return usuario
  }

  /**
   * Atualiza um usuário
   */
  async updateUsuario(id: string, data: UpdateUsuarioInput) {
    const prisma = await this.prismaPromise
    
    // Verifica se o usuário existe
    const usuario = await prisma.usuario.findUnique({
      where: { id }
    })

    if (!usuario) {
      throw new Error('Usuário não encontrado')
    }

    // Se está alterando email, verifica duplicação
    if (data.email && data.email.toLowerCase() !== usuario.email) {
      const existing = await prisma.usuario.findUnique({
        where: { email: data.email.toLowerCase() }
      })

      if (existing) {
        throw new Error('Email já cadastrado')
      }
    }

    // Prepara dados para atualização
    const updateData: any = {}

    if (data.nome) updateData.nome = data.nome
    if (data.email) updateData.email = data.email.toLowerCase()
    if (data.role) updateData.role = data.role
    if (data.ativo !== undefined) updateData.ativo = data.ativo

    // Se está alterando senha, faz o hash
    if (data.password) {
      const salt = await bcrypt.genSalt(10)
      updateData.passwordHash = await bcrypt.hash(data.password, salt)
    }

    // Atualiza
    const updated = await prisma.usuario.update({
      where: { id },
      data: updateData,
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
      }
    })

    return updated
  }

  /**
   * Desativa um usuário (soft delete)
   */
  async deactivateUsuario(id: string) {
    const prisma = await this.prismaPromise
    return prisma.usuario.update({
      where: { id },
      data: { ativo: false },
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
      }
    })
  }

  /**
   * Reativa um usuário
   */
  async reactivateUsuario(id: string) {
    const prisma = await this.prismaPromise
    return prisma.usuario.update({
      where: { id },
      data: { ativo: true },
      select: {
        id: true,
        nome: true,
        email: true,
        role: true,
        ativo: true,
        createdAt: true,
        updatedAt: true,
      }
    })
  }

  /**
   * Deleta um usuário (hard delete)
   * Cuidado: esta operação é irreversível
   */
  async deleteUsuario(id: string) {
    const prisma = await this.prismaPromise
    return prisma.usuario.delete({
      where: { id }
    })
  }
}
