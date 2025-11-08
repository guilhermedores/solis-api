const { Client } = require('pg');

async function removeTokenVinculacoes() {
  const client = new Client({
    host: 'localhost',
    port: 5432,
    database: 'solis_pdv',
    user: 'solis_user',
    password: 'solis123_secure_password'
  });

  try {
    await client.connect();
    console.log('✓ Conectado ao PostgreSQL\n');

    // Verificar se a tabela existe
    const tableExists = await client.query(`
      SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = 'token_vinculacoes'
      )
    `);

    if (!tableExists.rows[0].exists) {
      console.log('✓ Tabela token_vinculacoes já não existe');
      return;
    }

    console.log('Removendo tabela token_vinculacoes...');
    await client.query('DROP TABLE IF EXISTS public.token_vinculacoes CASCADE');
    console.log('✓ Tabela removida com sucesso');

    // Verificar tabelas restantes no public
    const tables = await client.query(`
      SELECT tablename 
      FROM pg_tables 
      WHERE schemaname = 'public'
      ORDER BY tablename
    `);

    console.log('\nTabelas no schema public:');
    tables.rows.forEach(t => console.log('  -', t.tablename));

    console.log('\n✓ Processo concluído!');

  } catch (error) {
    console.error('\n❌ Erro:', error.message);
    process.exit(1);
  } finally {
    await client.end();
  }
}

removeTokenVinculacoes();
