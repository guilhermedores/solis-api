# Database Scripts

Scripts SQL organizados para criação e manutenção do banco de dados Solis API.

## Estrutura

### Scripts de Criação (Executar em ordem)

1. **01-create-database.sql**
   - Cria extensões do PostgreSQL (uuid-ossp, pgcrypto)
   - Configura permissões básicas
   - **Execução**: Automática no Docker ou manual via psql

2. **02-create-tenants-table.sql**
   - Cria a tabela `public.tenants` para gestão de tenants
   - Cria índices de performance
   - Cria trigger de `updated_at`
   - **Execução**: Automática no Docker ou manual

3. **03-create-tenant-schema-function.sql**
   - Cria função `create_tenant_schema(schema_name)`
   - Provisiona schema completo do tenant com todas as tabelas:
     - users
     - tax_regimes
     - special_tax_regimes
     - companies
   - **Execução**: Automática no Docker ou manual

4. **04-seed-demo-tenant.sql**
   - Cria tenant de demonstração (subdomain: `demo`)
   - Popula tax_regimes e special_tax_regimes
   - Cria usuários padrão (admin, manager, operator)
   - Cria empresa de exemplo
   - **Execução**: Automática no Docker ou manual

### Scripts de Manutenção

- **99-rollback-all.sql**
  - Script de rollback completo
  - ⚠️ **ATENÇÃO**: Apaga TODOS os dados
  - Scripts comentados por segurança
  - **Execução**: Manual apenas, com extremo cuidado

## Execução Manual

### Via psql (Local ou Docker)

```bash
# Conectar ao PostgreSQL
psql -U solis_user -d solis_pdv

# Executar scripts em ordem
\i database/scripts/01-create-database.sql
\i database/scripts/02-create-tenants-table.sql
\i database/scripts/03-create-tenant-schema-function.sql
\i database/scripts/04-seed-demo-tenant.sql
```

### Via Docker Exec

```powershell
# Script único
docker exec -i solis-postgres psql -U solis_user -d solis_pdv < database/scripts/01-create-database.sql

# Todos os scripts em sequência
Get-ChildItem database/scripts/*.sql | Where-Object { $_.Name -notlike "99-*" } | Sort-Object Name | ForEach-Object {
    Write-Host "Executing $($_.Name)..." -ForegroundColor Cyan
    Get-Content $_.FullName | docker exec -i solis-postgres psql -U solis_user -d solis_pdv
}
```

## Execução Automática (Docker)

Quando o container PostgreSQL é criado pela primeira vez, o Docker executa automaticamente todos os scripts `.sql` da pasta `database/init/` em ordem alfabética.

Para habilitar execução automática:

1. **Copie os scripts para `database/init/`**:
```powershell
Copy-Item database/scripts/0*.sql database/init/
```

2. **Recrie o container**:
```powershell
docker-compose down -v
docker-compose up -d
```

## Criando Novo Tenant

Use a função `create_tenant_schema` para criar novos tenants:

```sql
-- 1. Inserir tenant na tabela public.tenants
INSERT INTO public.tenants (subdomain, legal_name, trade_name, cnpj, schema_name)
VALUES ('acme', 'Acme Corporation LTDA', 'Acme', '98765432000188', 'tenant_acme');

-- 2. Criar schema do tenant
SELECT create_tenant_schema('tenant_acme');

-- 3. Seed inicial (copiar estrutura do 04-seed-demo-tenant.sql e adaptar)
```

## Usuários Padrão (Demo Tenant)

| Email | Senha | Role |
|-------|-------|------|
| admin@demo.com | Admin@123 | admin |
| manager@demo.com | Manager@123 | manager |
| operator@demo.com | Operator@123 | operator |

## Verificação

### Verificar tenants criados
```sql
SELECT subdomain, legal_name, schema_name, active 
FROM public.tenants;
```

### Verificar tabelas de um tenant
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'tenant_demo';
```

### Verificar usuários de um tenant
```sql
SELECT name, email, role, active 
FROM tenant_demo.users;
```

## Troubleshooting

### Erro: "extension uuid-ossp does not exist"
```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

### Erro: "schema already exists"
```sql
-- Verificar se schema existe
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'tenant_demo';

-- Se necessário, dropar e recriar
DROP SCHEMA tenant_demo CASCADE;
SELECT create_tenant_schema('tenant_demo');
```

### Resetar banco completamente
```powershell
# Parar e remover container + volumes
docker-compose down -v

# Recriar do zero
docker-compose up -d
```

## Backup e Restore

### Backup completo
```powershell
docker exec solis-postgres pg_dump -U solis_user -F c -b -v -f /tmp/backup.dump solis_pdv
docker cp solis-postgres:/tmp/backup.dump ./backup.dump
```

### Restore
```powershell
docker cp ./backup.dump solis-postgres:/tmp/backup.dump
docker exec solis-postgres pg_restore -U solis_user -d solis_pdv -v /tmp/backup.dump
```

### Backup de um tenant específico
```powershell
docker exec solis-postgres pg_dump -U solis_user -n tenant_demo -F c solis_pdv > tenant_demo_backup.dump
```
