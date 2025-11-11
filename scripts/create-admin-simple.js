/**
 * Script simplificado para criar usuário admin usando pg direto
 * 
 * Uso:
 *   npm run create-admin <tenant>
 * 
 * Exemplos:
 *   npm run create-admin demo
 *   npm run create-admin empresa1
 * 
 * Para alterar email, senha, nome ou role, edite as constantes abaixo:
 */

require('dotenv').config()
const { Client } = require('pg')
const bcrypt = require('bcryptjs')
const crypto = require('crypto')

// ============================================
// CONFIGURAÇÃO PADRÃO DO USUÁRIO
// ============================================
const DEFAULT_EMAIL = 'admin@admin.com'
const DEFAULT_PASSWORD = 'admin123'
const DEFAULT_NAME = 'Administrador'
const DEFAULT_ROLE = 'admin' // admin, manager, operator

// ============================================
// CONFIGURAÇÃO DO BANCO
// ============================================
const DB_CONFIG = {
  host: 'localhost',
  port: 5432,
  database: 'solis_pdv',
  user: 'solis_user',
  password: process.env.DB_PASSWORD || 'solis123_secure_password'
}

// ============================================
// SCRIPT PRINCIPAL
// ============================================

async function createAdminUser() {
  console.log('='.repeat(70))
  console.log('Script de Criação de Usuário Admin')
  console.log('='.repeat(70))
  console.log()

  // Valida argumentos
  const args = process.argv.slice(2)
  if (args.length < 1) {
    console.error('❌ Faltando tenant!')
    console.error()
    console.error('Uso: npm run create-admin <tenant>')
    console.error('Exemplo: npm run create-admin demo')
    console.error()
    process.exit(1)
  }

  const tenant = args[0]
  const schemaName = `tenant_${tenant}`

  console.log('Configuração:')
  console.log(`   Tenant: ${tenant}`)
  console.log(`   Schema: ${schemaName}`)
  console.log(`   Nome:   ${DEFAULT_NAME}`)
  console.log(`   Email:  ${DEFAULT_EMAIL}`)
  console.log(`   Role:   ${DEFAULT_ROLE}`)
  console.log()

  const client = new Client(DB_CONFIG)

  try {
    // Conecta ao banco
    console.log('⏳ Conectando ao banco de dados...')
    await client.connect()
    console.log('✅ Conectado')
    console.log()

    // Verifica se o schema existe
    console.log(`🔍 Verificando schema "${schemaName}"...`)
    const schemaCheck = await client.query(
      `SELECT schema_name FROM information_schema.schemata WHERE schema_name = $1`,
      [schemaName]
    )

    if (schemaCheck.rows.length === 0) {
      console.error(`❌ Schema "${schemaName}" não existe!`)
      console.error()
      console.error('💡 Você precisa criar o schema primeiro:')
      console.error(`   CREATE SCHEMA IF NOT EXISTS ${schemaName};`)
      console.error()
      process.exit(1)
    }
    console.log('✅ Schema existe')
    console.log()

    // Define o schema
    await client.query(`SET search_path TO "${schemaName}"`)

    // Verifica se a tabela usuario existe
    console.log('🔍 Verificando tabela "users"...')
    const tableCheck = await client.query(
      `SELECT table_name FROM information_schema.tables 
       WHERE table_schema = $1 AND table_name = 'usuarios'`,
      [schemaName]
    )

    if (tableCheck.rows.length === 0) {
      console.error('❌ Tabela "users" não existe!')
      console.error()
      console.error('💡 Execute as migrations primeiro:')
      console.error('   npm run db:init')
      console.error()
      process.exit(1)
    }
    console.log('✅ Tabela existe')
    console.log()

    // Verifica se o email já existe
    console.log(`🔍 Verificando se email "${DEFAULT_EMAIL}" já existe...`)
    const emailCheck = await client.query(
      `SELECT id, nome, email, role, ativo FROM "${schemaName}"."usuarios" WHERE email = $1`,
      [DEFAULT_EMAIL.toLowerCase()]
    )

    if (emailCheck.rows.length > 0) {
      const existing = emailCheck.rows[0]
      console.log('⚠️  Usuário já existe!')
      console.log()
      console.log('Dados do usuário existente:')
      console.log(`   ID:    ${existing.id}`)
      console.log(`   Nome:  ${existing.nome}`)
      console.log(`   Email: ${existing.email}`)
      console.log(`   Role:  ${existing.role}`)
      console.log(`   Ativo: ${existing.ativo ? 'Sim' : 'Não'}`)
      console.log()
      console.log('💡 Use outro email ou delete o usuário existente primeiro.')
      console.log()
      process.exit(1)
    }
    console.log('✅ Email disponível')
    console.log()

    // Gera hash da senha
    console.log('🔐 Gerando hash da senha...')
    const salt = await bcrypt.genSalt(10)
    const passwordHash = await bcrypt.hash(DEFAULT_PASSWORD, salt)
    console.log('✅ Hash gerado')
    console.log()

    // Gera UUID
    const userId = crypto.randomUUID()

    // Cria o usuário
    console.log('💾 Criando usuário...')
    const result = await client.query(
      `INSERT INTO "${schemaName}"."usuarios" 
       (id, nome, email, password_hash, role, ativo, created_at, updated_at) 
       VALUES ($1, $2, $3, $4, $5, $6, NOW(), NOW())
       RETURNING id, nome, email, role, ativo, created_at`,
      [userId, DEFAULT_NAME, DEFAULT_EMAIL.toLowerCase(), passwordHash, DEFAULT_ROLE, true]
    )

    const usuario = result.rows[0]

    console.log('✅ Usuário criado com sucesso!')
    console.log()
    console.log('='.repeat(70))
    console.log('📋 Dados do usuário criado:')
    console.log('='.repeat(70))
    console.log(`ID:        ${usuario.id}`)
    console.log(`Nome:      ${usuario.nome}`)
    console.log(`Email:     ${usuario.email}`)
    console.log(`Role:      ${usuario.role}`)
    console.log(`Ativo:     ${usuario.ativo ? 'Sim' : 'Não'}`)
    console.log(`Criado em: ${usuario.createdAt}`)
    console.log('='.repeat(70))
    console.log()
    console.log('🎉 Processo concluído com sucesso!')
    console.log()
    console.log('🔑 Credenciais para login:')
    console.log(`   Tenant: ${tenant}`)
    console.log(`   Email:  ${DEFAULT_EMAIL}`)
    console.log(`   Senha:  ${DEFAULT_PASSWORD}`)
    console.log()
    console.log('💡 Para alterar as credenciais padrão, edite scripts/create-admin-simple.js')
    console.log()

  } catch (error) {
    console.error()
    console.error('❌ Erro:', error.message)
    console.error()
    process.exit(1)
  } finally {
    await client.end()
  }
}

// Executa
createAdminUser()
