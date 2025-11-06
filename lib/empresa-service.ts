/**
 * Serviço de gerenciamento de Empresas
 * Gerencia dados da empresa para emissão de cupons fiscais
 * Tabela armazenada no schema de cada tenant
 */

import { getPrismaClient } from './prisma'
import type { PrismaClient } from '@prisma/client'

export interface CreateEmpresaInput {
  // Dados da Empresa
  razaoSocial: string
  nomeFantasia?: string
  cnpj: string
  inscricaoEstadual?: string
  inscricaoMunicipal?: string
  
  // Endereço
  logradouro: string
  numero: string
  complemento?: string
  bairro: string
  cidade: string
  uf: string
  cep: string
  
  // Contato
  telefone?: string
  email?: string
  site?: string
  
  // Regime Tributário
  regimeTributario: 'simples_nacional' | 'lucro_presumido' | 'lucro_real'
  
  // Informações Fiscais
  certificadoDigital?: string
  senhaCertificado?: string
  ambienteFiscal?: 'producao' | 'homologacao'
  
  // Logo
  logo?: string
}

export interface UpdateEmpresaInput {
  razaoSocial?: string
  nomeFantasia?: string
  inscricaoEstadual?: string
  inscricaoMunicipal?: string
  logradouro?: string
  numero?: string
  complemento?: string
  bairro?: string
  cidade?: string
  uf?: string
  cep?: string
  telefone?: string
  email?: string
  site?: string
  regimeTributario?: 'simples_nacional' | 'lucro_presumido' | 'lucro_real'
  certificadoDigital?: string
  senhaCertificado?: string
  ambienteFiscal?: 'producao' | 'homologacao'
  logo?: string
  ativo?: boolean
}

export class EmpresaService {
  private tenant: string
  private prismaPromise: Promise<any> // Using any to avoid type issues with dynamic tenant schemas

  constructor(tenant: string) {
    this.tenant = tenant
    this.prismaPromise = getPrismaClient(tenant)
  }

  private async getPrisma(): Promise<any> {
    return await this.prismaPromise
  }

  /**
   * Criar nova empresa
   */
  async createEmpresa(data: CreateEmpresaInput): Promise<any> {
    const prisma = await this.getPrisma()
    
    // Validar CNPJ
    const cnpjLimpo = data.cnpj.replace(/[^\d]/g, '')
    if (cnpjLimpo.length !== 14) {
      throw new Error('CNPJ inválido. Deve conter 14 dígitos.')
    }

    // Validar UF
    const ufsValidas = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']
    if (!ufsValidas.includes(data.uf.toUpperCase())) {
      throw new Error('UF inválida.')
    }

    // Validar CEP
    const cepLimpo = data.cep.replace(/[^\d]/g, '')
    if (cepLimpo.length !== 8) {
      throw new Error('CEP inválido. Deve conter 8 dígitos.')
    }

    const empresa = await (await this.getPrisma()).empresa.create({
      data: {
        razaoSocial: data.razaoSocial,
        nomeFantasia: data.nomeFantasia,
        cnpj: data.cnpj,
        inscricaoEstadual: data.inscricaoEstadual,
        inscricaoMunicipal: data.inscricaoMunicipal,
        logradouro: data.logradouro,
        numero: data.numero,
        complemento: data.complemento,
        bairro: data.bairro,
        cidade: data.cidade,
        uf: data.uf.toUpperCase(),
        cep: data.cep,
        telefone: data.telefone,
        email: data.email,
        site: data.site,
        regimeTributario: data.regimeTributario || 'simples_nacional',
        certificadoDigital: data.certificadoDigital,
        senhaCertificado: data.senhaCertificado,
        ambienteFiscal: data.ambienteFiscal || 'homologacao',
        logo: data.logo,
      },
    })

    return empresa
  }

  /**
   * Buscar empresa por ID
   */
  async getEmpresaById(id: string): Promise<any> {
    return await (await this.getPrisma()).empresa.findUnique({
      where: { id },
    })
  }

  /**
   * Buscar empresa por CNPJ
   */
  async getEmpresaByCnpj(cnpj: string): Promise<any> {
    const cnpjLimpo = cnpj.replace(/[^\d]/g, '')
    
    return await (await this.getPrisma()).empresa.findFirst({
      where: {
        cnpj: {
          contains: cnpjLimpo
        }
      },
    })
  }

  /**
   * Listar empresas
   */
  async listEmpresas(
    page: number = 1,
    limit: number = 50,
    ativo?: boolean
  ): Promise<{
    data: any[]
    total: number
    page: number
    totalPages: number
  }> {
    const where: any = {}

    if (ativo !== undefined) {
      where.ativo = ativo
    }

    const [data, total] = await Promise.all([
      (await this.getPrisma()).empresa.findMany({
        where,
        skip: (page - 1) * limit,
        take: limit,
        orderBy: { createdAt: 'desc' },
      }),
      (await this.getPrisma()).empresa.count({ where }),
    ])

    return {
      data,
      total,
      page,
      totalPages: Math.ceil(total / limit),
    }
  }

  /**
   * Atualizar empresa
   */
  async updateEmpresa(id: string, data: UpdateEmpresaInput): Promise<any> {
    // Validações se campos forem fornecidos
    if (data.uf) {
      const ufsValidas = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']
      if (!ufsValidas.includes(data.uf.toUpperCase())) {
        throw new Error('UF inválida.')
      }
      data.uf = data.uf.toUpperCase()
    }

    if (data.cep) {
      const cepLimpo = data.cep.replace(/[^\d]/g, '')
      if (cepLimpo.length !== 8) {
        throw new Error('CEP inválido. Deve conter 8 dígitos.')
      }
    }

    return await (await this.getPrisma()).empresa.update({
      where: { id },
      data: {
        ...data,
        updatedAt: new Date(),
      },
    })
  }

  /**
   * Desativar empresa
   */
  async deactivateEmpresa(id: string): Promise<any> {
    return await (await this.getPrisma()).empresa.update({
      where: { id },
      data: {
        ativo: false,
      },
    })
  }

  /**
   * Reativar empresa
   */
  async reactivateEmpresa(id: string): Promise<any> {
    return await (await this.getPrisma()).empresa.update({
      where: { id },
      data: {
        ativo: true,
      },
    })
  }

  /**
   * Deletar empresa permanentemente
   */
  async deleteEmpresa(id: string): Promise<void> {
    await (await this.getPrisma()).empresa.delete({
      where: { id },
    })
  }

  /**
   * Obter dados da empresa para cupom fiscal
   * Remove dados sensíveis (certificado, senha)
   */
  async getEmpresaForCupom(id: string): Promise<any> {
    const empresa = await (await this.getPrisma()).empresa.findUnique({
      where: { id },
      select: {
        id: true,
        razaoSocial: true,
        nomeFantasia: true,
        cnpj: true,
        inscricaoEstadual: true,
        inscricaoMunicipal: true,
        logradouro: true,
        numero: true,
        complemento: true,
        bairro: true,
        cidade: true,
        uf: true,
        cep: true,
        telefone: true,
        email: true,
        site: true,
        regimeTributario: true,
        ambienteFiscal: true,
        logo: true,
        ativo: true,
        // NÃO retorna: certificadoDigital, senhaCertificado
      },
    })

    return empresa
  }
}

// Instância singleton
// Instância singleton (será removida, as rotas devem instanciar com tenant)
// export const empresaService = new EmpresaService()

