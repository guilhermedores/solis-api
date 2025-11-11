/**
 * Script Node.js para inicializar o banco de dados
 * Alternativa ao PowerShell que não depende do psql no PATH
 */

require('dotenv').config()
const { Client } = require('pg')
const fs = require('fs')
const path = require('path')

// Configuração do banco
const config = {
  host: process.env.DB_HOST || 'localhost',
  port: parseInt(process.env.DB_PORT || '5432'),
  database: process.env.DB_NAME || 'solis_pdv',
  user: process.env.DB_USER || 'solis_user',
  password: process.env.DB_PASSWORD || 'solis123_secure_password'
}

console.log('======================================================================')
console.log('Inicializacao do Banco de Dados')
console.log('======================================================================')
console.log('')
console.log('Configuracao:')
console.log(`  Host: ${config.host}`)
console.log(`  Port: ${config.port}`)
console.log(`  Database: ${config.database}`)
console.log(`  User: ${config.user}`)
console.log('')

async function runSQLFile(client, filePath) {
  const fileName = path.basename(filePath)
  console.log(`Executando: ${fileName}`)
  
  try {
    const sql = fs.readFileSync(filePath, 'utf8')
    await client.query(sql)
    console.log(`OK: ${fileName} executado com sucesso`)
    console.log('')
    return true
  } catch (error) {
    console.error(`ERRO ao executar ${fileName}:`)
    console.error(error.message)
    console.log('')
    return false
  }
}

async function initDatabase() {
  const client = new Client(config)
  
  try {
    // Conectar
    console.log('Conectando ao banco de dados...')
    await client.connect()
    console.log('Conexao bem-sucedida!')
    console.log('')
    
    // Executar scripts na ordem
    console.log('Executando scripts de inicializacao...')
    console.log('')
    
    const scripts = [
      path.join(__dirname, '../database/init/01-create-database.sql'),
      path.join(__dirname, '../database/init/02-create-demo-tenant.sql'),
      path.join(__dirname, '../database/init/03-create-tenant-tables.sql'),
      path.join(__dirname, '../database/init/04-seed-demo-data.sql')
    ]
    
    for (const script of scripts) {
      if (!fs.existsSync(script)) {
        console.error(`ERRO: Arquivo nao encontrado: ${script}`)
        process.exit(1)
      }
      
      const success = await runSQLFile(client, script)
      if (!success) {
        process.exit(1)
      }
    }
    
    console.log('======================================================================')
    console.log('Banco de dados inicializado com sucesso!')
    console.log('======================================================================')
    console.log('')
    console.log('Estrutura criada:')
    console.log('  - Schema public: tabela tenants')
    console.log('  - Schema tenant_demo: tabelas users, empresas')
    console.log('  - Tenant demo criado e ativo')
    console.log('  - Empresa demo criada')
    console.log('  - Usuario admin criado (admin@admin.com / admin123)')
    console.log('')
    console.log('Pronto para usar!')
    console.log('  - Inicie a API: npm run dev')
    console.log('  - Teste o login com as credenciais acima')
    console.log('')
    
  } catch (error) {
    console.error('')
    console.error('ERRO ao conectar ao banco de dados:')
    console.error(error.message)
    console.error('')
    console.error('Verifique:')
    console.error('  - PostgreSQL esta rodando?')
    console.error('  - Credenciais estao corretas no .env?')
    console.error(`  - Banco de dados '${config.database}' existe?`)
    console.error('')
    process.exit(1)
  } finally {
    await client.end()
  }
}

initDatabase()
