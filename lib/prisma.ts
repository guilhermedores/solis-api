import { PrismaClient } from '@prisma/client'
import { getTenantDatabaseConfig } from './database'

// Cache de clientes Prisma por tenant
const prismaClients = new Map<string, PrismaClient>()

/**
 * Cliente Prisma para o schema público (tenants)
 * Usado para gerenciar tenants e tokens de vinculação
 */
export const prismaPublic = new PrismaClient({
  datasources: {
    db: {
      url: process.env.DATABASE_URL
    }
  }
})

/**
 * Retorna uma instância do Prisma Client configurada para o tenant específico
 * Implementa connection pooling com cache de clientes por tenant
 */
export async function getPrismaClient(tenant: string): Promise<PrismaClient> {
  // Retorna cliente em cache se existir
  if (prismaClients.has(tenant)) {
    return prismaClients.get(tenant)!
  }

  // Obtém configuração do banco de dados para o tenant
  const { config, isolation } = getTenantDatabaseConfig(tenant)
  
  // Criar connection string baseado na configuração
  let connectionString: string
  if (isolation.type === 'DATABASE') {
    connectionString = `postgresql://${config.user}:${config.password}@${config.host}:${config.port}/${config.database}`
  } else {
    connectionString = process.env.DATABASE_URL || ''
  }
  
  // Para isolamento por SCHEMA, adicionar o schema na connection string
  if (isolation.type === 'SCHEMA' && isolation.schema) {
    connectionString = `${connectionString}?schema=${isolation.schema}`
  }
  
  // Cria novo cliente Prisma
  const prisma = new PrismaClient({
    datasources: {
      db: {
        url: connectionString
      }
    }
  })

  // Armazena no cache
  prismaClients.set(tenant, prisma)
  
  const detail = isolation.type === 'SCHEMA' ? isolation.schema : isolation.database
  console.log(`[Prisma] Cliente criado para tenant "${tenant}" (${isolation.type}: ${detail})`)
  
  return prisma
}

/**
 * Fecha todas as conexões Prisma (útil para testes ou shutdown graceful)
 */
export async function disconnectAllPrismaClients() {
  for (const [tenant, prisma] of prismaClients.entries()) {
    await prisma.$disconnect()
    console.log(`[Prisma] Cliente desconectado para tenant "${tenant}"`)
  }
  prismaClients.clear()
}

/**
 * Middleware para configurar schema em cada query (alternativa mais robusta)
 * Use este se precisar garantir que o search_path seja aplicado em TODA query
 */
export function createPrismaClientWithMiddleware(tenant: string): PrismaClient {
  const { config, isolation } = getTenantDatabaseConfig(tenant)
  
  // Criar connection string baseado na configuração
  let connectionString: string
  if (isolation.type === 'DATABASE') {
    connectionString = `postgresql://${config.user}:${config.password}@${config.host}:${config.port}/${config.database}`
  } else {
    connectionString = process.env.DATABASE_URL || ''
  }
  
  // Para isolamento por SCHEMA, adicionar o schema na connection string
  if (isolation.type === 'SCHEMA' && isolation.schema) {
    connectionString = `${connectionString}?schema=${isolation.schema}`
  }

  console.log(`[Prisma] Connection string para tenant "${tenant}": ${connectionString.replace(/:[^:@]+@/, ':***@')}`)

  const prisma = new PrismaClient({
    datasources: {
      db: {
        url: connectionString
      }
    }
  })

  return prisma
}
