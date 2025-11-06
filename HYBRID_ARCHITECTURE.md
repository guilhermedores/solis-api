# 🏗️ Arquitetura Híbrida Multi-Tenant

O Solis API suporta **arquitetura híbrida** de multi-tenancy, permitindo escolher entre:
- **Schema Compartilhado** (padrão) - Ideal para a maioria dos clientes
- **Banco Dedicado** - Para clientes Enterprise/Premium

## 📁 Schema Compartilhado (Padrão)

**Quando usar:**
- Pequenas e médias empresas (99% dos clientes)
- Clientes nos planos Basic e Professional
- Custo-benefício otimizado
- Gerenciamento simplificado

**Características:**
- Todos os tenants no mesmo banco PostgreSQL
- Isolamento via schemas (`tenant_cliente1`, `tenant_cliente2`, etc)
- Um único backup para todos
- Migrations aplicadas uma vez
- Connection pooling eficiente

**Configuração:**
```env
# .env.local
DB_HOST=localhost
DB_PORT=5432
DB_NAME=solis_pdv
DB_USER=solis_user
DB_PASSWORD=solis123
```

**Estrutura do banco:**
```
solis_pdv (database)
├── public (schema)
│   └── tenants (tabela global)
├── tenant_cliente1 (schema)
│   ├── produtos
│   ├── vendas
│   └── ...
├── tenant_cliente2 (schema)
│   ├── produtos
│   ├── vendas
│   └── ...
└── tenant_demo (schema)
    ├── produtos
    ├── vendas
    └── ...
```

## 🗄️ Banco Dedicado (Enterprise)

**Quando usar:**
- Clientes Enterprise/Premium
- Requisitos de compliance (LGPD, GDPR)
- Clientes muito grandes (>100GB de dados)
- Necessidade de escalar individualmente
- Dados em regiões geográficas diferentes
- SLA customizado

**Características:**
- PostgreSQL dedicado para o tenant
- Isolamento total (segurança máxima)
- Backup/restore independente
- Pode estar em servidor/região diferente
- Escala independentemente

**Configuração - Opção 1 (Connection String):**
```env
# .env.local

# Tenant "megacorp" com banco dedicado
DB_MEGACORP_URL=postgresql://megacorp_user:senha@db-megacorp.empresa.com:5432/megacorp_db

# Tenant "bigretail" com banco dedicado em outra região
DB_BIGRETAIL_URL=postgresql://retail_user:senha@db-retail-sp.empresa.com:5432/bigretail_db
```

**Configuração - Opção 2 (Variáveis Individuais):**
```env
# .env.local

# Tenant "megacorp" com banco dedicado
DB_MEGACORP_HOST=db-megacorp.empresa.com
DB_MEGACORP_PORT=5432
DB_MEGACORP_NAME=megacorp_db
DB_MEGACORP_USER=megacorp_user
DB_MEGACORP_PASSWORD=senha_segura_123
```

## 🔄 Migração Schema → Banco Dedicado

Quando um cliente cresce e precisa de banco dedicado:

### 1. Provisionar novo banco PostgreSQL
```bash
# Criar servidor dedicado (AWS RDS, Azure PostgreSQL, etc)
# Ou container Docker dedicado:
docker run -d \
  --name solis-megacorp \
  -e POSTGRES_DB=megacorp_db \
  -e POSTGRES_USER=megacorp_user \
  -e POSTGRES_PASSWORD=senha \
  -p 5433:5432 \
  postgres:15-alpine
```

### 2. Executar migrations no novo banco
```bash
# Aplicar estrutura de tabelas
psql -h db-megacorp.empresa.com -U megacorp_user -d megacorp_db -f migrations/create-tables.sql
```

### 3. Migrar dados do schema para o banco
```bash
# Dump do schema específico
pg_dump -h localhost -U solis_user -d solis_pdv \
  -n tenant_megacorp \
  --data-only \
  > megacorp_data.sql

# Restore no novo banco
psql -h db-megacorp.empresa.com -U megacorp_user -d megacorp_db -f megacorp_data.sql
```

### 4. Atualizar configuração
```env
# Adicionar variável de ambiente
DB_MEGACORP_URL=postgresql://megacorp_user:senha@db-megacorp.empresa.com:5432/megacorp_db
```

### 5. Reiniciar API
```bash
# O código automaticamente detecta e usa o banco dedicado
npm run dev
```

### 6. Testar
```bash
curl http://localhost:3000/api/health?tenant=megacorp

# Response:
{
  "tenant": "megacorp",
  "isValid": true,
  "isolation": {
    "type": "DATABASE",
    "detail": "megacorp_db",
    "description": "🗄️  Banco de dados dedicado (isolamento total)"
  }
}
```

### 7. Limpar schema antigo (após validação)
```sql
-- Após confirmar que tudo funciona no banco dedicado
DROP SCHEMA tenant_megacorp CASCADE;
```

## 🧪 Como Testar

### Testar tenant com schema compartilhado:
```bash
# Tenant "demo" usa schema compartilhado
curl "http://localhost:3000/api/health?tenant=demo"

# Response:
{
  "isolation": {
    "type": "SCHEMA",
    "detail": "tenant_demo",
    "description": "📁 Schema compartilhado (custo-benefício otimizado)"
  }
}
```

### Testar tenant com banco dedicado:
```bash
# Configurar variável de ambiente primeiro:
# DB_ENTERPRISE_URL=postgresql://...

curl "http://localhost:3000/api/health?tenant=enterprise"

# Response:
{
  "isolation": {
    "type": "DATABASE",
    "detail": "enterprise_db",
    "description": "🗄️  Banco de dados dedicado (isolamento total)"
  }
}
```

## 📊 Comparação

| Aspecto | Schema Compartilhado | Banco Dedicado |
|---------|---------------------|----------------|
| **Custo** | 💰 Baixo | 💰💰💰 Alto |
| **Isolamento** | ⭐⭐⭐ Bom | ⭐⭐⭐⭐⭐ Máximo |
| **Performance** | ⚡ Boa (compartilhada) | ⚡⚡⚡ Excelente (dedicada) |
| **Escalabilidade** | 📈 Limitada ao servidor | 📈📈📈 Ilimitada |
| **Gerenciamento** | ✅ Simples | ⚠️ Complexo |
| **Backup/Restore** | 📦 Global | 📦 Individual |
| **Ideal para** | SMB (99% dos clientes) | Enterprise (1% dos clientes) |

## 💡 Recomendações

### Plano Basic
- Schema compartilhado
- Até 2 terminais
- Até 10.000 produtos
- Até 50.000 vendas/mês

### Plano Professional
- Schema compartilhado
- Até 5 terminais
- Até 50.000 produtos
- Até 200.000 vendas/mês

### Plano Premium
- Schema compartilhado
- Até 10 terminais
- Até 100.000 produtos
- Até 500.000 vendas/mês

### Plano Enterprise
- **Banco dedicado** 🗄️
- Terminais ilimitados
- Produtos ilimitados
- Vendas ilimitadas
- SLA customizado
- Suporte prioritário

## 🔧 Troubleshooting

### Como saber qual tipo de isolamento um tenant usa?
```bash
curl "http://localhost:3000/api/health?tenant=cliente1"
```

### Como verificar todos os tenants ativos?
```sql
SELECT 
  subdomain,
  company_name,
  plan,
  CASE 
    WHEN EXISTS (
      SELECT 1 FROM pg_database WHERE datname = 'solis_' || subdomain
    ) THEN 'DATABASE'
    ELSE 'SCHEMA'
  END as isolation_type
FROM tenants 
WHERE active = true;
```

### Como migrar tenant de volta para schema compartilhado?
1. Dump do banco dedicado
2. Restore no schema do banco principal
3. Remover variável `DB_TENANTNAME_URL`
4. Reiniciar API
5. Desligar banco dedicado

## 🚀 Próximos Passos

- [ ] Criar script automatizado de migração schema → database
- [ ] Implementar dashboard de gerenciamento de tenants
- [ ] Adicionar métricas de uso por tenant
- [ ] Implementar rate limiting por tenant
- [ ] Criar alertas de uso (storage, connections, etc)
- [ ] Implementar backup/restore por tenant via API
