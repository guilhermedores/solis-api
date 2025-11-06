const { Client } = require('pg');
const fs = require('fs');
const path = require('path');

async function runMigration() {
  const client = new Client({
    host: 'localhost',
    port: 5432,
    database: 'solis_pdv',
    user: 'solis_user',
    password: 'solis123_secure_password'
  });

  try {
    await client.connect();
    console.log('Conectado ao PostgreSQL');

    // Verificar se a tabela empresas já existe
    const checkTable = await client.query(`
      SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'empresas'
      );
    `);

    if (checkTable.rows[0].exists) {
      console.log('✓ Tabela empresas já existe');
    } else {
      console.log('Criando tabela empresas...');
      
      // Ler e executar o script SQL
      const sqlPath = path.join(__dirname, '03-empresas.sql');
      const sql = fs.readFileSync(sqlPath, 'utf8');
      
      await client.query(sql);
      console.log('✓ Tabela empresas criada com sucesso');
    }

    // Verificar quantidade de registros
    const countResult = await client.query('SELECT COUNT(*) FROM public.empresas');
    console.log(`✓ Total de empresas: ${countResult.rows[0].count}`);

    // Listar empresas
    const empresas = await client.query(`
      SELECT id, razao_social, cnpj, ativo 
      FROM public.empresas 
      ORDER BY created_at DESC
    `);
    
    console.log('\nEmpresas cadastradas:');
    empresas.rows.forEach(emp => {
      console.log(`  - ${emp.razao_social} (CNPJ: ${emp.cnpj}) ${emp.ativo ? '✓' : '✗'}`);
    });

  } catch (error) {
    console.error('Erro:', error.message);
    process.exit(1);
  } finally {
    await client.end();
  }
}

runMigration();
