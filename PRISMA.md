# Prisma ORM - Solis API

Este projeto agora suporta **Prisma ORM** como alternativa ao uso direto do driver `pg` (node-postgres).

## 🚀 Vantagens do Prisma

✅ **Type-Safety Total**: TypeScript completo com autocomplete  
✅ **Migrations Automatizadas**: Gerenciamento de versões do schema  
✅ **Query Builder Intuitivo**: Sintaxe declarativa e limpa  
✅ **Relacionamentos Simplificados**: Include/select automático  
✅ **Multi-Tenant Support**: Compatível com nossa arquitetura híbrida  

---

## 📦 Estrutura

```
solis-api/
├── prisma/
│   ├── schema.prisma          # Schema do banco (modelos)
│   └── migrations/            # Histórico de migrations (será criado)
├── lib/
│   ├── prisma.ts              # Cliente Prisma multi-tenant
│   └── database.ts            # Driver pg (node-postgres)
├── app/api/
│   ├── produtos/              # Endpoints usando pg direto
│   └── produtos-prisma/       # Endpoints usando Prisma ORM
```

---

## 🔧 Comandos Prisma

### Gerar Cliente (após alterar schema)
```bash
npx prisma generate
```

### Criar Migration
```bash
npx prisma migrate dev --name nome_da_migration
```

### Aplicar Migrations em Produção
```bash
npx prisma migrate deploy
```

### Visualizar Banco no Prisma Studio
```bash
npx prisma studio
```

### Pull do Schema Existente (gerar schema.prisma do banco)
```bash
npx prisma db pull
```

### Push do Schema sem Migration (desenvolvimento rápido)
```bash
npx prisma db push
```

---

## 📝 Schema Atual

O schema em `prisma/schema.prisma` reflete a estrutura do banco:

- **Produto** → produtos
- **ProdutoPreco** → produto_precos
- **FormaPagamento** → formas_pagamento
- **Venda** → vendas
- **VendaItem** → venda_itens
- **VendaPagamento** → venda_pagamentos

### Exemplo de Modelo

```prisma
model Produto {
  id              String   @id @default(uuid())
  codigoBarras    String?  @map("codigo_barras")
  nome            String
  unidadeMedida   String   @map("unidade_medida")
  ativo           Boolean  @default(true)
  createdAt       DateTime @default(now()) @map("created_at")
  updatedAt       DateTime @updatedAt @map("updated_at")

  // Relacionamentos
  precos          ProdutoPreco[]
  vendaItens      VendaItem[]

  @@map("produtos")
  @@index([nome])
}
```

---

## 🏗️ Multi-Tenant com Prisma

### Usando o Cliente Prisma Multi-Tenant

```typescript
import { getTenant } from '@/lib/tenant'
import { getPrismaClient } from '@/lib/prisma'

export async function GET(request: NextRequest) {
  // 1. Identifica o tenant
  const tenant = await getTenant()
  
  // 2. Obtém cliente Prisma configurado para o tenant
  const prisma = await getPrismaClient(tenant)
  
  // 3. Usa Prisma normalmente
  const produtos = await prisma.produto.findMany({
    where: { ativo: true },
    include: {
      precos: {
        where: { ativo: true }
      }
    },
    orderBy: { nome: 'asc' }
  })
  
  return NextResponse.json({ produtos })
}
```

### Isolamento por Schema (padrão)

O cliente Prisma automaticamente executa `SET search_path` para o schema correto:

- `tenant_demo` → Schema tenant_demo
- `tenant_cliente1` → Schema tenant_cliente1
- `tenant_cliente2` → Schema tenant_cliente2

### Isolamento por Banco Dedicado

Se o tenant tiver variável `DB_TENANTNAME_URL` configurada:

```env
DB_CLIENTE1_URL="postgresql://user:pass@servidor1.com:5432/cliente1_db"
```

O Prisma criará um cliente conectado ao banco dedicado.

---

## 🔄 Comparação: pg vs Prisma

### Com `pg` (node-postgres)

```typescript
const query = `
  SELECT p.*, pp.preco_venda, pp.preco_custo
  FROM produtos p
  LEFT JOIN produto_precos pp ON p.id = pp.produto_id
  WHERE p.ativo = $1
  ORDER BY p.nome ASC
`
const produtos = await queryWithTenant(tenant, query, [true])
```

### Com Prisma ORM

```typescript
const produtos = await prisma.produto.findMany({
  where: { ativo: true },
  include: {
    precos: {
      where: { ativo: true },
      take: 1
    }
  },
  orderBy: { nome: 'asc' }
})
```

---

## 🎯 Quando Usar Cada Um?

### Use **Prisma** quando:
- ✅ Operações CRUD simples e diretas
- ✅ Relacionamentos complexos (include/select)
- ✅ Type-safety é crítico
- ✅ Queries comuns e previsíveis

### Use **pg direto** quando:
- ✅ Queries muito complexas (CTEs, window functions)
- ✅ Performance crítica (queries otimizadas manualmente)
- ✅ Bulk operations (INSERT/UPDATE em massa)
- ✅ Queries dinâmicas com SQL raw

---

## 📚 Operações Comuns

### Listar com Filtros e Paginação

```typescript
const produtos = await prisma.produto.findMany({
  where: {
    ativo: true,
    OR: [
      { nome: { contains: 'arroz', mode: 'insensitive' } },
      { codigoBarras: { contains: '7891234' } }
    ]
  },
  include: {
    precos: {
      where: { ativo: true },
      orderBy: { createdAt: 'desc' },
      take: 1
    }
  },
  orderBy: { nome: 'asc' },
  skip: 0,
  take: 50
})
```

### Criar com Relacionamento

```typescript
const produto = await prisma.produto.create({
  data: {
    nome: 'Arroz Integral 1kg',
    unidadeMedida: 'UN',
    codigoBarras: '7891234567890',
    precos: {
      create: {
        precoVenda: 12.90,
        precoCusto: 8.50
      }
    }
  },
  include: {
    precos: true
  }
})
```

### Atualizar

```typescript
const produto = await prisma.produto.update({
  where: { id: '123' },
  data: {
    nome: 'Arroz Integral 1kg Premium',
    precos: {
      create: {
        precoVenda: 14.90,
        precoCusto: 9.50
      }
    }
  },
  include: {
    precos: {
      where: { ativo: true }
    }
  }
})
```

### Soft Delete

```typescript
await prisma.produto.update({
  where: { id: '123' },
  data: { ativo: false }
})
```

### Buscar por ID

```typescript
const produto = await prisma.produto.findUnique({
  where: { id: '123' },
  include: {
    precos: {
      where: { ativo: true }
    }
  }
})
```

### Contar Registros

```typescript
const total = await prisma.produto.count({
  where: { ativo: true }
})
```

### Transações

```typescript
await prisma.$transaction(async (tx) => {
  // Criar produto
  const produto = await tx.produto.create({
    data: { nome: 'Produto', unidadeMedida: 'UN' }
  })
  
  // Criar preço
  await tx.produtoPreco.create({
    data: {
      produtoId: produto.id,
      precoVenda: 10.00
    }
  })
})
```

---

## 🛠️ Migrations

### Workflow de Desenvolvimento

1. **Altere o schema.prisma**
```prisma
model Produto {
  // ... campos existentes
  
  // Novo campo
  estoque Int @default(0)
}
```

2. **Crie a migration**
```bash
npx prisma migrate dev --name adicionar_estoque
```

3. **Prisma vai:**
   - Gerar SQL da migration
   - Aplicar no banco de desenvolvimento
   - Atualizar o Prisma Client

### Aplicar Migrations em Produção

```bash
# Deploy de todas as migrations pendentes
npx prisma migrate deploy
```

---

## 📊 Prisma Studio

Interface visual para visualizar e editar dados:

```bash
npx prisma studio
```

Abre em `http://localhost:5555` com interface para:
- Visualizar todos os dados das tabelas
- Editar registros diretamente
- Executar filtros e buscas
- Navegar relacionamentos

---

## 🔐 Boas Práticas

### 1. Sempre use getPrismaClient()

```typescript
// ✅ CORRETO
const prisma = await getPrismaClient(tenant)

// ❌ ERRADO (não respeita multi-tenant)
import { PrismaClient } from '@prisma/client'
const prisma = new PrismaClient()
```

### 2. Use Transações para Operações Complexas

```typescript
await prisma.$transaction(async (tx) => {
  // Todas as operações aqui são atômicas
  await tx.produto.create({ ... })
  await tx.produtoPreco.create({ ... })
})
```

### 3. Inclua Apenas o Necessário

```typescript
// ✅ CORRETO
const produto = await prisma.produto.findUnique({
  where: { id },
  include: {
    precos: { where: { ativo: true }, take: 1 }
  }
})

// ❌ EVITE (carrega tudo)
const produto = await prisma.produto.findUnique({
  where: { id },
  include: {
    precos: true,
    vendaItens: true
  }
})
```

### 4. Use Skip/Take para Paginação

```typescript
// Paginação eficiente
const produtos = await prisma.produto.findMany({
  skip: (page - 1) * pageSize,
  take: pageSize
})
```

---

## 🧪 Testando

### Endpoint de Teste

Criamos `/api/produtos-prisma` como exemplo:

```bash
# GET - Listar produtos
curl "http://localhost:3000/api/produtos-prisma?tenant=demo&search=arroz"

# POST - Criar produto
curl -X POST "http://localhost:3000/api/produtos-prisma?tenant=demo" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Arroz Integral 1kg",
    "unidadeMedida": "UN",
    "codigoBarras": "7891234567890",
    "precoVenda": 12.90,
    "precoCusto": 8.50
  }'
```

Acesse também via Swagger: `http://localhost:3000/docs`

---

## 🤝 Convivência com pg

Os dois métodos podem coexistir:

- **Prisma**: Para 80% das operações (CRUD comum)
- **pg**: Para queries complexas específicas

Exemplo:

```typescript
// Use Prisma para operações simples
const produtos = await prisma.produto.findMany()

// Use pg para queries complexas
const relatorio = await queryWithTenant(tenant, `
  WITH vendas_mensais AS (
    SELECT DATE_TRUNC('month', data_venda) as mes,
           SUM(valor_final) as total
    FROM vendas
    GROUP BY mes
  )
  SELECT * FROM vendas_mensais
  ORDER BY mes DESC
`)
```

---

## 📖 Documentação Oficial

- [Prisma Docs](https://www.prisma.io/docs)
- [Schema Reference](https://www.prisma.io/docs/reference/api-reference/prisma-schema-reference)
- [Client API](https://www.prisma.io/docs/reference/api-reference/prisma-client-reference)
- [Migrations](https://www.prisma.io/docs/concepts/components/prisma-migrate)

---

## 🎉 Próximos Passos

1. **Teste o endpoint**: `GET /api/produtos-prisma?tenant=demo`
2. **Crie mais endpoints**: Siga o exemplo de `produtos-prisma`
3. **Explore migrations**: Adicione novos campos ao schema
4. **Use Prisma Studio**: Visualize os dados em tempo real
