# 🗄️ Inicialização do Banco de Dados

## Arquitetura Multi-tenant

O banco de dados segue a arquitetura **Schema per Tenant**, onde:

- **Schema `public`**: Contém apenas a tabela `Tenant`
- **Schema `tenant_*`**: Um schema separado para cada tenant com suas tabelas

### Estrutura:
```
solis_pdv (database)
├── public (schema)
│   └── Tenant (tabela única)
├── tenant_demo (schema)
│   ├── Usuario
│   └── Empresa
└── tenant_<subdomain> (schema por tenant)
    ├── Usuario
    └── Empresa
```

## 🚀 Inicialização

### Windows (PowerShell)
```powershell
cd database
.\init-database.ps1
```

### Manual (passo a passo)
```powershell
# 1. Criar estrutura base e tabela Tenant
psql -U solis_user -d solis_pdv -f database/init/01-create-database.sql

# 2. Criar tenant demo
psql -U solis_user -d solis_pdv -f database/init/02-create-demo-tenant.sql

# 3. Criar tabelas do tenant
psql -U solis_user -d solis_pdv -f database/init/03-create-tenant-tables.sql

# 4. Popular dados iniciais
psql -U solis_user -d solis_pdv -f database/init/04-seed-demo-data.sql
```

## 📝 Scripts de Inicialização

### 01-create-database.sql
- Limpa e recria schema public
- Cria tabela `Tenant`
- Cria funções e triggers para `updatedAt`

### 02-create-demo-tenant.sql
- Insere tenant 'demo'
- Cria schema `tenant_demo`

### 03-create-tenant-tables.sql
- Cria tabelas no schema do tenant:
  - `Usuario` (usuários com roles)
  - `Empresa` (filiais/lojas)

### 04-seed-demo-data.sql
- Popula empresa demo

## 🔑 Criar Usuário Admin

Após inicializar o banco, crie um usuário admin:

```bash
npm run create-admin demo
```

Credenciais padrão (configuradas no script):
- Email: `admin@admin.com`
- Senha: `admin123`

## 🆕 Criar Novo Tenant

Para criar um novo tenant:

1. Insira na tabela Tenant:
```sql
INSERT INTO public."Tenant" (subdomain, name, active)
VALUES ('novo-tenant', 'Nome do Tenant', true);
```

2. Crie o schema:
```sql
CREATE SCHEMA tenant_novotenant;
GRANT ALL ON SCHEMA tenant_novotenant TO solis_user;
```

3. Copie e adapte o script de tabelas:
```powershell
# Copiar script
Copy-Item database\init\03-create-tenant-tables.sql temp-novotenant.sql

# Editar o arquivo substituindo 'tenant_demo' por 'tenant_novotenant'
# Depois executar:
psql -U solis_user -d solis_pdv -f temp-novotenant.sql

# Remover arquivo temporário
Remove-Item temp-novotenant.sql
```

4. Criar usuário admin:
```bash
npm run create-admin novotenant
```

## 🔍 Verificar Estrutura

```sql
-- Listar todos os schemas
\dn

-- Listar tenants
SELECT * FROM public."Tenant";

-- Listar tabelas de um tenant
\dt tenant_demo.*

-- Verificar usuários de um tenant
SELECT id, nome, email, role FROM tenant_demo."Usuario";

-- Verificar empresas de um tenant
SELECT id, "nomeFantasia", cnpj FROM tenant_demo."Empresa";
```

## ⚠️ Observações Importantes

1. **Schema Public**
   - Contém **apenas** a tabela `Tenant`
   - Não adicione outras tabelas aqui

2. **Schemas de Tenant**
   - Cada tenant tem seu próprio schema isolado
   - Formato do nome: `tenant_<subdomain>`
   - Todas as tabelas de dados ficam nos schemas de tenant

3. **Nomenclatura**
   - Tabelas usam PascalCase (ex: `Usuario`, `Empresa`)
   - Colunas usam camelCase (ex: `nomeFantasia`, `createdAt`)
   - Segue convenção do Prisma

4. **Prisma**
   - Não execute migrations no schema public
   - Migrations devem ser executadas nos schemas de tenant
   - Use `prisma migrate deploy` especificando o schema

## 🔧 Troubleshooting

### Erro: "password authentication failed"
- Verifique as credenciais no arquivo `.env`
- Confirme que o usuário `solis_user` existe no PostgreSQL

### Erro: "database does not exist"
- Crie o banco de dados primeiro:
```sql
CREATE DATABASE solis_pdv;
```

### Erro: "permission denied"
- Garanta que o usuário tem permissões:
```sql
GRANT ALL PRIVILEGES ON DATABASE solis_pdv TO solis_user;
```

### Erro ao executar psql
- Instale PostgreSQL client tools
- Adicione psql ao PATH do sistema
