// Configuração de conexão com banco de dados por tenant
// Suporta arquitetura híbrida: schemas compartilhados OU bancos dedicados
import { Pool } from 'pg'

// Cache de pools de conexão por tenant
const pools: Record<string, Pool> = {}

/**
 * Tipo de isolamento do tenant
 */
export type TenantIsolationType = 'SCHEMA' | 'DATABASE'

/**
 * Informações sobre o tipo de isolamento do tenant
 */
interface TenantIsolationInfo {
  type: TenantIsolationType
  schema?: string // Apenas para tipo SCHEMA
  database?: string // Apenas para tipo DATABASE
}

interface DatabaseConfig {
  host: string
  port: number
  database: string
  user: string
  password: string
  max?: number
  idleTimeoutMillis?: number
  connectionTimeoutMillis?: number
}

// Pegar configuração do banco para um tenant específico
export function getTenantDatabaseConfig(tenant: string): { config: DatabaseConfig; isolation: TenantIsolationInfo } {
  // Verifica se o tenant tem banco de dados DEDICADO
  // Formato das variáveis de ambiente:
  // - DB_CLIENTE1_URL (connection string completa)
  // - DB_CLIENTE1_HOST, DB_CLIENTE1_PORT, DB_CLIENTE1_NAME, etc (individuais)
  
  const dedicatedUrl = process.env[`DB_${tenant.toUpperCase()}_URL`]
  const dedicatedHost = process.env[`DB_${tenant.toUpperCase()}_HOST`]

  if (dedicatedUrl || dedicatedHost) {
    // ========================================================================
    // BANCO DEDICADO - Isolamento total por database
    // ========================================================================
    // Use cases:
    // - Clientes Enterprise/Premium que pagam por servidor dedicado
    // - Requisitos de compliance/regulamentação (LGPD, GDPR)
    // - Tenants muito grandes que precisam escalar individualmente
    // - Dados em regiões geográficas diferentes
    
    if (dedicatedUrl) {
      // Parse da connection string: postgresql://user:pass@host:port/database
      const url = new URL(dedicatedUrl)
      return {
        config: {
          host: url.hostname,
          port: parseInt(url.port || '5432'),
          database: url.pathname.slice(1), // Remove o '/' inicial
          user: url.username,
          password: url.password,
          max: 20,
          idleTimeoutMillis: 30000,
          connectionTimeoutMillis: 2000,
        },
        isolation: {
          type: 'DATABASE',
          database: url.pathname.slice(1),
        },
      }
    } else {
      // Configuração individual por variáveis
      const database = process.env[`DB_${tenant.toUpperCase()}_NAME`] || `solis_${tenant}`
      return {
        config: {
          host: dedicatedHost!,
          port: parseInt(process.env[`DB_${tenant.toUpperCase()}_PORT`] || '5432'),
          database,
          user: process.env[`DB_${tenant.toUpperCase()}_USER`] || 'solis_user',
          password: process.env[`DB_${tenant.toUpperCase()}_PASSWORD`] || 'solis123',
          max: 20,
          idleTimeoutMillis: 30000,
          connectionTimeoutMillis: 2000,
        },
        isolation: {
          type: 'DATABASE',
          database,
        },
      }
    }
  }

  // ========================================================================
  // SCHEMA COMPARTILHADO - Padrão para a maioria dos tenants
  // ========================================================================
  // Use cases:
  // - Pequenas e médias empresas (a maioria dos clientes)
  // - Custo-benefício otimizado
  // - Gerenciamento simplificado (1 banco para centenas de tenants)
  // - Fácil escalar horizontalmente adicionando novos tenants
  
  const baseConfig = {
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT || '5432'),
    user: process.env.DB_USER || 'postgres',
    password: process.env.DB_PASSWORD || 'postgres',
    max: 20, // Máximo de conexões no pool
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 2000,
  }
  
  return {
    config: {
      ...baseConfig,
      database: process.env.DB_NAME || 'solis',
    },
    isolation: {
      type: 'SCHEMA',
      schema: `tenant_${tenant}`,
    },
  }
}

// Cache de metadata de isolamento por tenant
const isolationInfo: Record<string, TenantIsolationInfo> = {}

// Obter pool de conexão para um tenant
export function getTenantPool(tenant: string): Pool {
  // Se já existe um pool para este tenant, retorna
  if (pools[tenant]) {
    return pools[tenant]
  }
  
  // Criar novo pool
  const { config, isolation } = getTenantDatabaseConfig(tenant)
  const pool = new Pool(config)
  
  // Armazenar metadata de isolamento
  isolationInfo[tenant] = isolation
  
  // Event handlers para debug
  pool.on('connect', () => {
    const isolationType = isolation.type === 'DATABASE' ? '🗄️  Banco Dedicado' : '📁 Schema Compartilhado'
    const detail = isolation.type === 'DATABASE' ? isolation.database : isolation.schema
    console.log(`[DB] ${isolationType} - Nova conexão para tenant: ${tenant} (${detail})`)
  })
  
  pool.on('error', (err) => {
    console.error(`[DB] Erro no pool do tenant ${tenant}:`, err)
  })
  
  // Armazenar no cache
  pools[tenant] = pool
  
  return pool
}

// Obter informações sobre o tipo de isolamento do tenant
export function getTenantIsolation(tenant: string): TenantIsolationInfo {
  if (!isolationInfo[tenant]) {
    // Se ainda não foi carregado, buscar configuração
    const { isolation } = getTenantDatabaseConfig(tenant)
    isolationInfo[tenant] = isolation
  }
  return isolationInfo[tenant]
}

// Executar query com tenant context
export async function queryWithTenant<T = any>(
  tenant: string,
  text: string,
  params?: any[]
): Promise<T> {
  const pool = getTenantPool(tenant)
  const isolation = getTenantIsolation(tenant)
  
  let fullQuery = text
  
  // Para schemas compartilhados, adicionar SET search_path
  // Para bancos dedicados, não é necessário (já está no banco certo)
  if (isolation.type === 'SCHEMA' && tenant !== 'default') {
    const schemaPrefix = `SET search_path TO ${isolation.schema}, public;`
    fullQuery = `${schemaPrefix} ${text}`
  }
  
  try {
    const result = await pool.query(fullQuery, params)
    return result.rows as T
  } catch (error) {
    console.error(`[DB] Erro ao executar query para tenant ${tenant}:`, error)
    throw error
  }
}

// Cleanup de pools (útil para graceful shutdown)
export async function closeAllPools(): Promise<void> {
  const closePromises = Object.entries(pools).map(async ([tenant, pool]) => {
    console.log(`[DB] Fechando pool para tenant: ${tenant}`)
    await pool.end()
  })
  
  await Promise.all(closePromises)
  
  // Limpar cache
  Object.keys(pools).forEach(key => delete pools[key])
}

// Verificar se tenant existe e está ativo
export async function validateTenant(tenant: string): Promise<boolean> {
  if (tenant === 'default') return true
  
  try {
    // Query para verificar se o tenant existe na tabela de tenants
    const result = await queryWithTenant<{ exists: boolean }[]>(
      'default', // Usa o schema default para buscar info de tenants
      'SELECT EXISTS(SELECT 1 FROM tenants WHERE subdomain = $1 AND active = true) as exists',
      [tenant]
    )
    
    return result[0]?.exists || false
  } catch (error) {
    console.error(`[DB] Erro ao validar tenant ${tenant}:`, error)
    return false
  }
}
