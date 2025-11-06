# Solis API - Multi-Tenant REST API# Solis API - Multi-Tenant REST API# Solis API - Multi-Tenant System# Solis API - Multi-Tenant SystemThis is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).



**API backend REST** do sistema Solis PDV com suporte a **arquitetura híbrida multi-tenant** baseado em subdomínios.



> ⚠️ Este projeto é **exclusivamente API** - não possui páginas web. Para frontend, veja `solis-pwa` e `solis-admin`.**API backend REST** do sistema Solis PDV com suporte a **arquitetura híbrida multi-tenant** baseado em subdomínios.



## 🚀 Tecnologias



- **Next.js 15 App Router** - API Routes only (sem páginas)> ⚠️ Este projeto é **exclusivamente API** - não possui páginas web. Para frontend, veja `solis-pwa` e `solis-admin`.API backend do sistema Solis PDV com suporte a **arquitetura híbrida multi-tenant** baseado em subdomínios.

- **TypeScript** - Type-safe development  

- **PostgreSQL** - Banco de dados relacional

- **Multi-Tenancy Híbrida** - Schemas compartilhados OU bancos dedicados

- **Connection Pooling** - pg (node-postgres)## 🚀 Tecnologias

- **Swagger/OpenAPI 3.0** - Documentação interativa da API



## 🏗️ Arquitetura Multi-Tenant

- **Next.js 15 App Router** - API Routes only (sem páginas)## 🚀 TecnologiasAPI backend do sistema Solis PDV com suporte a multi-tenancy baseado em subdomínios.## Getting Started

O sistema suporta **dois tipos de isolamento**:

- **TypeScript** - Type-safe development  

### 📁 Schema Compartilhado (Padrão - 99% dos clientes)

- Todos os tenants no mesmo banco PostgreSQL- **PostgreSQL** - Banco de dados relacional

- Isolamento via schemas (`tenant_cliente1`, `tenant_cliente2`, etc)

- **Ideal para:** Pequenas e médias empresas- **Multi-Tenancy Híbrida** - Schemas compartilhados OU bancos dedicados

- **Vantagens:** Custo-benefício, gerenciamento simples, backup unificado

- **Planos:** Basic, Professional, Premium- **Connection Pooling** - pg (node-postgres)- **Next.js 15** - Framework React com App Router



### 🗄️ Banco Dedicado (Enterprise - 1% dos clientes)

- PostgreSQL dedicado para o tenant

- Isolamento total de dados## 🏗️ Arquitetura Multi-Tenant- **TypeScript** - Type-safe development

- **Ideal para:** Clientes Enterprise, compliance (LGPD/GDPR), grandes volumes

- **Vantagens:** Máxima segurança, escala individual, SLA customizado

- **Plano:** Enterprise

O sistema suporta **dois tipos de isolamento**:- **PostgreSQL** - Banco de dados relacional## 🚀 TecnologiasFirst, run the development server:

**📖 Documentação completa:** [HYBRID_ARCHITECTURE.md](./HYBRID_ARCHITECTURE.md)



## 🌐 Identificação de Tenant

### 📁 Schema Compartilhado (Padrão - 99% dos clientes)- **Tailwind CSS** - Estilização

O sistema identifica automaticamente o tenant através do subdomínio:

- Todos os tenants no mesmo banco PostgreSQL

**Produção:**

- `cliente1.solis.com.br` → Tenant: `cliente1` (schema compartilhado)- Isolamento via schemas (`tenant_cliente1`, `tenant_cliente2`, etc)- **Multi-Tenancy Híbrida** - Schemas compartilhados OU bancos dedicados

- `megacorp.solis.com.br` → Tenant: `megacorp` (banco dedicado)

- `api.solis.com.br` → Tenant: `default`- **Ideal para:** Pequenas e médias empresas



**Desenvolvimento Local:**- **Vantagens:** Custo-benefício, gerenciamento simples, backup unificado



```bash- **Planos:** Basic, Professional, Premium

# Via Query Param

http://localhost:3000/api/health?tenant=demo## 🏗️ Arquitetura Multi-Tenant- **Next.js 15** - Framework React com App Router```bash



# Via Header### 🗄️ Banco Dedicado (Enterprise - 1% dos clientes)

curl -H "x-tenant: demo" http://localhost:3000/api/health

```- PostgreSQL dedicado para o tenant



## 🚀 Getting Started- Isolamento total de dados



### Pré-requisitos- **Ideal para:** Clientes Enterprise, compliance (LGPD/GDPR), grandes volumesO sistema suporta **dois tipos de isolamento**:- **TypeScript** - Type-safe developmentnpm run dev



- Node.js 20+- **Vantagens:** Máxima segurança, escala individual, SLA customizado

- PostgreSQL 15+

- Docker (opcional)- **Plano:** Enterprise



### Instalação



```bash**📖 Documentação completa:** [HYBRID_ARCHITECTURE.md](./HYBRID_ARCHITECTURE.md)### 📁 Schema Compartilhado (Padrão - 99% dos clientes)- **PostgreSQL** - Banco de dados relacional# or

# Instalar dependências

npm install



# Copiar variáveis de ambiente## 🌐 Identificação de Tenant- Todos os tenants no mesmo banco PostgreSQL

cp .env.local.example .env.local



# Editar .env.local com suas credenciais

```O sistema identifica automaticamente o tenant através do subdomínio:- Isolamento via schemas (`tenant_cliente1`, `tenant_cliente2`, etc)- **Tailwind CSS** - Estilizaçãoyarn dev



### Configuração - Schema Compartilhado (Padrão)



```env**Produção:**- **Ideal para:** Pequenas e médias empresas

# .env.local

DB_HOST=localhost- `cliente1.solis.com.br` → Tenant: `cliente1` (schema compartilhado)

DB_PORT=5432

DB_NAME=solis_pdv- `megacorp.solis.com.br` → Tenant: `megacorp` (banco dedicado)- **Vantagens:** Custo-benefício, gerenciamento simples, backup unificado- **Multi-Tenancy** - Isolamento por schema/tenant# or

DB_USER=solis_user

DB_PASSWORD=solis123- `api.solis.com.br` → Tenant: `default`

```

- **Planos:** Basic, Professional, Premium

### Configuração - Banco Dedicado (Enterprise)

**Desenvolvimento Local:**

```env

# .env.localpnpm dev



# Tenant "megacorp" com banco dedicado```bash

DB_MEGACORP_URL=postgresql://megacorp_user:senha@db-megacorp.com:5432/megacorp_db

# Via Query Param### 🗄️ Banco Dedicado (Enterprise - 1% dos clientes)

# OU (configuração individual):

DB_MEGACORP_HOST=db-megacorp.comhttp://localhost:3000/api/health?tenant=demo

DB_MEGACORP_PORT=5432

DB_MEGACORP_NAME=megacorp_db- PostgreSQL dedicado para o tenant## 🏗️ Arquitetura Multi-Tenant# or

DB_MEGACORP_USER=megacorp_user

DB_MEGACORP_PASSWORD=senha# Via Header

```

curl -H "x-tenant: demo" http://localhost:3000/api/health- Isolamento total de dados

### Executar

```

```bash

# Desenvolvimento- **Ideal para:** Clientes Enterprise, compliance (LGPD/GDPR), grandes volumesbun dev

npm run dev

## 🚀 Getting Started

# Build

npm run build- **Vantagens:** Máxima segurança, escala individual, SLA customizado



# Produção### Pré-requisitos

npm start

```- **Plano:** EnterpriseO sistema identifica automaticamente o tenant através do subdomínio:```



A API estará disponível em **http://localhost:3000**- Node.js 20+



## 📊 API Documentation- PostgreSQL 15+



### 📖 Documentação Interativa (Swagger UI)- Docker (opcional)



Acesse a documentação completa e teste os endpoints interativamente:**📖 Documentação completa:** [HYBRID_ARCHITECTURE.md](./HYBRID_ARCHITECTURE.md)



- **Swagger UI:** http://localhost:3000/docs### Instalação

- **OpenAPI 3.0 Spec (JSON):** http://localhost:3000/api/docs



![Swagger UI](https://via.placeholder.com/800x400/4A90E2/ffffff?text=Swagger+UI+Preview)

```bash

### Endpoints Disponíveis

# Instalar dependências## 🌐 Identificação de Tenant- `cliente1.solis.com.br` → Tenant: `cliente1`Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

#### System

npm install

```bash

# Informações da API

GET /api

# Copiar variáveis de ambiente

# OpenAPI Specification

GET /api/docscp .env.local.example .env.localO sistema identifica automaticamente o tenant através do subdomínio:- `cliente2.solis.com.br` → Tenant: `cliente2`

```



#### Health Check

# Editar .env.local com suas credenciais

```bash

# Verificar status e tipo de isolamento do tenant```

GET /api/health?tenant=demo

**Produção:**- `solis.com.br` → Tenant: `default`You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

Response:

{### Configuração - Schema Compartilhado (Padrão)

  "tenant": "demo",

  "isValid": true,- `cliente1.solis.com.br` → Tenant: `cliente1` (schema compartilhado)

  "isolation": {

    "type": "SCHEMA",```env

    "detail": "tenant_demo",

    "description": "📁 Schema compartilhado"# .env.local- `megacorp.solis.com.br` → Tenant: `megacorp` (banco dedicado)

  },

  "timestamp": "2025-11-03T12:00:00.000Z",DB_HOST=localhost

  "message": "Connected to tenant: demo"

}DB_PORT=5432- `solis.com.br` → Tenant: `default`

```

DB_NAME=solis_pdv

#### Produtos (em desenvolvimento)

DB_USER=solis_user### Desenvolvimento LocalThis project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

```bash

GET    /api/produtos?tenant=demoDB_PASSWORD=solis123

POST   /api/produtos?tenant=demo

GET    /api/produtos/:id?tenant=demo```**Desenvolvimento Local:**

PUT    /api/produtos/:id?tenant=demo

DELETE /api/produtos/:id?tenant=demo

```

### Configuração - Banco Dedicado (Enterprise)

#### Vendas (em desenvolvimento)



```bash

GET  /api/vendas?tenant=demo```env```bash

POST /api/vendas?tenant=demo

GET  /api/vendas/:id?tenant=demo# .env.local

```

# Via Query ParamPara testar localmente, use query params ou headers:## Learn More

## 🐳 Docker

# Tenant "megacorp" com banco dedicado

### Subir PostgreSQL

DB_MEGACORP_URL=postgresql://megacorp_user:senha@db-megacorp.com:5432/megacorp_dbhttp://localhost:3000/?tenant=demo

```bash

# Com docker-compose (na raiz do projeto)

cd ..

docker-compose up -d postgres# OU (configuração individual):http://localhost:3000/api/health?tenant=cliente1



# Aguardar inicialização (cria schemas automaticamente)DB_MEGACORP_HOST=db-megacorp.com

docker logs -f solis-postgres

```DB_MEGACORP_PORT=5432



### Subir APIDB_MEGACORP_NAME=megacorp_db



```bashDB_MEGACORP_USER=megacorp_user# Via Header```bashTo learn more about Next.js, take a look at the following resources:

# Build e run

docker-compose up -d --build solis-apiDB_MEGACORP_PASSWORD=senha



# Logs```curl -H "x-tenant: demo" http://localhost:3000/api/health

docker logs -f solis-api

```



## 🧪 Testes### Executar```# Via Query Param



### Swagger UI

```bash

# Abrir documentação interativa```bash

open http://localhost:3000/docs

```# Desenvolvimento



### Testar API rootnpm run dev## 🚀 Getting Startedhttp://localhost:3000/api/health?tenant=cliente1- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.

```bash

curl http://localhost:3000/api

```

# Build

### Testar tenant com schema compartilhado

```bashnpm run build

curl "http://localhost:3000/api/health?tenant=demo"

### Pré-requisitos- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

# Deve retornar: "type": "SCHEMA"

```# Produção



### Testar tenant com banco dedicadonpm start

```bash

# 1. Criar banco dedicado```

docker run -d \

  --name solis-enterprise \- Node.js 20+# Via Header (usando curl)

  --network solis-pdv-network \

  -e POSTGRES_DB=enterprise_db \A API estará disponível em **http://localhost:3000**

  -e POSTGRES_USER=enterprise_user \

  -e POSTGRES_PASSWORD=enterprise_pass \- PostgreSQL 15+

  -p 5433:5432 \

  postgres:15-alpine## 📊 Endpoints



# 2. Configurar .env.local- Docker (opcional)curl -H "x-tenant: cliente1" http://localhost:3000/api/healthYou can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

# DB_ENTERPRISE_URL=postgresql://enterprise_user:enterprise_pass@localhost:5433/enterprise_db

### Root - Informações da API

# 3. Testar

curl "http://localhost:3000/api/health?tenant=enterprise"```bash



# Deve retornar: "type": "DATABASE"GET /api

```

### Instalação```

## 📁 Estrutura do Projeto

Response:

```

solis-api/{

├── app/

│   ├── docs/  "name": "Solis API",

│   │   └── page.tsx              # Swagger UI page

│   ├── api/  "version": "1.0.0",```bash## Deploy on Vercel

│   │   ├── route.ts              # Root endpoint (informações da API)

│   │   ├── docs/  "description": "API backend do sistema Solis PDV com multi-tenancy",

│   │   │   └── route.ts          # OpenAPI specification (JSON)

│   │   ├── health/  "endpoints": {# Instalar dependências

│   │   │   └── route.ts          # Health check endpoint

│   │   ├── produtos/             # CRUD produtos (em desenvolvimento)    "health": "/api/health?tenant={tenant}",

│   │   └── vendas/               # CRUD vendas (em desenvolvimento)

│   └── layout.tsx                # Layout mínimo (sem UI)    "produtos": "/api/produtos?tenant={tenant}",npm install## 📁 Estrutura do Projeto

├── lib/

│   ├── database.ts               # 🔥 Connection pooling híbrido    "vendas": "/api/vendas?tenant={tenant}"

│   ├── tenant.ts                 # Utilitários de tenant

│   └── swagger.ts                # 🔥 Configuração OpenAPI/Swagger  }

├── middleware.ts                 # 🔥 Extração de tenant

├── .env.local                    # Variáveis de ambiente}

├── HYBRID_ARCHITECTURE.md        # 📖 Documentação detalhada

├── next.config.ts                # Config otimizada para API```# Copiar variáveis de ambienteThe easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

├── package.json

└── tsconfig.json

```

### Health Checkcp .env.local.example .env.local

## 🔧 Variáveis de Ambiente

```bash

```env

# Banco de dados compartilhado (padrão)GET /api/health?tenant=demo```

DB_HOST=localhost

DB_PORT=5432

DB_NAME=solis_pdv

DB_USER=solis_userResponse:# Editar .env.local com suas credenciais

DB_PASSWORD=solis123

{

# Banco dedicado para tenant específico (opcional)

DB_MEGACORP_URL=postgresql://user:pass@host:port/database  "tenant": "demo",```solis-api/Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.



# Next.js  "isValid": true,

NODE_ENV=development

PORT=3000  "isolation": {



# Autenticação (futuro)    "type": "SCHEMA",

JWT_SECRET=your-secret-key

NEXTAUTH_SECRET=your-secret-key    "detail": "tenant_demo",### Configuração - Schema Compartilhado (Padrão)├── app/

NEXTAUTH_URL=http://localhost:3000

```    "description": "📁 Schema compartilhado (custo-benefício otimizado)"



## 🎯 Roadmap  },│   ├── api/



- [x] Middleware de extração de tenant  "timestamp": "2025-11-03T12:00:00.000Z",

- [x] Connection pooling por tenant

- [x] Suporte a schemas compartilhados  "message": "Connected to tenant: demo"```env│   │   ├── health/          # Endpoint de health check

- [x] Suporte a bancos dedicados

- [x] Health check endpoint}

- [x] API root endpoint

- [x] Configuração API-only (sem UI)```# .env.local│   │   ├── produtos/        # CRUD de produtos

- [x] **Swagger/OpenAPI 3.0 documentation**

- [x] **Swagger UI interativo**

- [ ] CRUD de Produtos

- [ ] CRUD de Vendas### Produtos (em desenvolvimento)DB_HOST=localhost│   │   └── vendas/          # CRUD de vendas

- [ ] Autenticação JWT

- [ ] Rate limiting por tenant```bash

- [ ] Métricas de uso

- [ ] Testes automatizados (Jest + Supertest)GET    /api/produtos?tenant=demoDB_PORT=5432│   ├── page.tsx             # Página inicial

- [ ] Script de migração schema → database

- [ ] Backup/restore por tenantPOST   /api/produtos?tenant=demo



## 📚 Documentação AdicionalGET    /api/produtos/:id?tenant=demoDB_NAME=solis_pdv│   └── layout.tsx



- [Swagger UI (Interativo)](http://localhost:3000/docs) - Documentação visual e testávelPUT    /api/produtos/:id?tenant=demo

- [OpenAPI Spec](http://localhost:3000/api/docs) - Especificação em JSON

- [Arquitetura Híbrida Detalhada](./HYBRID_ARCHITECTURE.md)DELETE /api/produtos/:id?tenant=demoDB_USER=solis_user├── lib/

- [Exemplo de Tenant Dedicado](./.env.dedicated-tenant-example)

```

## 🔒 Segurança

DB_PASSWORD=solis123│   ├── tenant.ts            # Utilitários para tenant context

- CORS configurado por tenant

- Rate limiting (em desenvolvimento)### Vendas (em desenvolvimento)

- JWT authentication (em desenvolvimento)

- SQL injection protection via parameterized queries```bash```│   └── database.ts          # Gerenciamento de conexões por tenant

- Isolamento de dados por tenant (schema ou database)

GET  /api/vendas?tenant=demo

## 🤝 Contribuindo

POST /api/vendas?tenant=demo├── middleware.ts            # Middleware de multi-tenancy

1. Fork o projeto

2. Crie uma branch (`git checkout -b feature/AmazingFeature`)GET  /api/vendas/:id?tenant=demo

3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)

4. Push para a branch (`git push origin feature/AmazingFeature`)```### Configuração - Banco Dedicado (Enterprise)└── .env.local               # Configurações locais

5. Abra um Pull Request



## 📝 License

## 🐳 Docker```

Este projeto é proprietário - © 2025 Solis PDV



## ✨ Learn More

### Subir PostgreSQL```env

- [Next.js API Routes](https://nextjs.org/docs/app/building-your-application/routing/route-handlers)

- [OpenAPI 3.0 Specification](https://swagger.io/specification/)

- [Swagger UI](https://swagger.io/tools/swagger-ui/)

- [PostgreSQL Multi-Tenancy](https://www.postgresql.org/docs/current/ddl-schemas.html)```bash# .env.local## 🔧 Configuração

- [Database Connection Pooling](https://node-postgres.com/apis/pool)

# Com docker-compose (na raiz do projeto)

cd ..

docker-compose up -d postgres

# Tenant "megacorp" com banco dedicado1. **Instalar dependências:**

# Aguardar inicialização (cria schemas automaticamente)

docker logs -f solis-postgresDB_MEGACORP_URL=postgresql://megacorp_user:senha@db-megacorp.com:5432/megacorp_db```bash

```

npm install

### Subir API

# OU (configuração individual):```

```bash

# Build e runDB_MEGACORP_HOST=db-megacorp.com

docker-compose up -d --build solis-api

DB_MEGACORP_PORT=54322. **Configurar variáveis de ambiente:**

# Logs

docker logs -f solis-apiDB_MEGACORP_NAME=megacorp_db```bash

```

DB_MEGACORP_USER=megacorp_usercp .env.example .env.local

## 🧪 Testes

DB_MEGACORP_PASSWORD=senha# Editar .env.local com suas configurações

### Testar API root

```bash``````

curl http://localhost:3000/api

```



### Testar tenant com schema compartilhado### Executar3. **Iniciar em desenvolvimento:**

```bash

curl "http://localhost:3000/api/health?tenant=demo"```bash



# Deve retornar: "type": "SCHEMA"```bashnpm run dev

```

# Desenvolvimento```

### Testar tenant com banco dedicado

```bashnpm run dev

# 1. Criar banco dedicado

docker run -d \## 🌐 Endpoints

  --name solis-enterprise \

  --network solis-pdv-network \# Build

  -e POSTGRES_DB=enterprise_db \

  -e POSTGRES_USER=enterprise_user \npm run build### Health Check

  -e POSTGRES_PASSWORD=enterprise_pass \

  -p 5433:5432 \```

  postgres:15-alpine

# ProduçãoGET /api/health

# 2. Configurar .env.local

# DB_ENTERPRISE_URL=postgresql://enterprise_user:enterprise_pass@localhost:5433/enterprise_dbnpm start```



# 3. Testar```

curl "http://localhost:3000/api/health?tenant=enterprise"

Retorna informações sobre o tenant ativo e status da API.

# Deve retornar: "type": "DATABASE"

```A API estará disponível em [http://localhost:3000](http://localhost:3000)



## 📁 Estrutura do Projeto### Produtos



```## 📊 Endpoints```

solis-api/

├── app/GET    /api/produtos        # Listar produtos

│   ├── api/

│   │   ├── route.ts               # Root endpoint (informações da API)### Health CheckPOST   /api/produtos        # Criar produto

│   │   ├── health/

│   │   │   └── route.ts          # Health check endpoint```bashGET    /api/produtos/:id    # Obter produto

│   │   ├── produtos/              # CRUD produtos (em desenvolvimento)

│   │   └── vendas/                # CRUD vendas (em desenvolvimento)GET /api/health?tenant=demoPUT    /api/produtos/:id    # Atualizar produto

│   └── layout.tsx                 # Layout mínimo (sem UI)

├── lib/DELETE /api/produtos/:id    # Deletar produto

│   ├── database.ts                # 🔥 Connection pooling híbrido

│   └── tenant.ts                  # Utilitários de tenantResponse:```

├── middleware.ts                  # 🔥 Extração de tenant

├── .env.local                     # Variáveis de ambiente{

├── HYBRID_ARCHITECTURE.md         # 📖 Documentação detalhada

├── next.config.ts                 # Config otimizada para API  "tenant": "demo",### Vendas

├── package.json

└── tsconfig.json  "isValid": true,```

```

  "isolation": {GET    /api/vendas          # Listar vendas

## 🔧 Variáveis de Ambiente

    "type": "SCHEMA",POST   /api/vendas          # Criar venda

```env

# Banco de dados compartilhado (padrão)    "detail": "tenant_demo",GET    /api/vendas/:id      # Obter venda

DB_HOST=localhost

DB_PORT=5432    "description": "📁 Schema compartilhado (custo-benefício otimizado)"```

DB_NAME=solis_pdv

DB_USER=solis_user  },

DB_PASSWORD=solis123

  "timestamp": "2025-11-03T12:00:00.000Z",## 🗄️ Estratégia de Banco de Dados

# Banco dedicado para tenant específico (opcional)

DB_MEGACORP_URL=postgresql://user:pass@host:port/database  "message": "Connected to tenant: demo"



# Next.js}O sistema usa **schemas PostgreSQL separados** para cada tenant:

NODE_ENV=development

PORT=3000```



# Autenticação (futuro)- `tenant_cliente1` - Schema do cliente 1

JWT_SECRET=your-secret-key

NEXTAUTH_SECRET=your-secret-key### Produtos (em desenvolvimento)- `tenant_cliente2` - Schema do cliente 2

NEXTAUTH_URL=http://localhost:3000

``````bash- `public` - Schema default e tabela de tenants



## 🎯 RoadmapGET /api/produtos?tenant=demo



- [x] Middleware de extração de tenantPOST /api/produtos?tenant=demoCada requisição automaticamente usa o schema correto baseado no tenant identificado.

- [x] Connection pooling por tenant

- [x] Suporte a schemas compartilhadosPUT /api/produtos/:id?tenant=demo

- [x] Suporte a bancos dedicados

- [x] Health check endpointDELETE /api/produtos/:id?tenant=demo## 🔐 Segurança

- [x] API root endpoint

- [x] Configuração API-only (sem UI)```

- [ ] CRUD de Produtos

- [ ] CRUD de Vendas- Isolamento completo de dados por tenant

- [ ] Autenticação JWT

- [ ] Rate limiting por tenant### Vendas (em desenvolvimento)- Validação de tenant em todas as requisições

- [ ] Métricas de uso

- [ ] OpenAPI/Swagger documentation```bash- JWT para autenticação

- [ ] Script de migração schema → database

- [ ] Backup/restore por tenantGET /api/vendas?tenant=demo- CORS configurável



## 📚 Documentação AdicionalPOST /api/vendas?tenant=demo- Environment variables para secrets



- [Arquitetura Híbrida Detalhada](./HYBRID_ARCHITECTURE.md)GET /api/vendas/:id?tenant=demo

- [Exemplo de Tenant Dedicado](./.env.dedicated-tenant-example)

```## 📝 Scripts Disponíveis

## 🔒 Segurança



- CORS configurado por tenant

- Rate limiting (em desenvolvimento)## 🐳 Docker```bash

- JWT authentication (em desenvolvimento)

- SQL injection protection via parameterized queriesnpm run dev          # Desenvolvimento

- Isolamento de dados por tenant (schema ou database)

### Subir PostgreSQLnpm run build        # Build de produção

## 🤝 Contribuindo

npm run start        # Iniciar produção

1. Fork o projeto

2. Crie uma branch (`git checkout -b feature/AmazingFeature`)```bashnpm run lint         # Linting

3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)

4. Push para a branch (`git push origin feature/AmazingFeature`)# Com docker-compose (na raiz do projeto)```

5. Abra um Pull Request

cd ..

## 📝 License

docker-compose up -d postgres## 🚢 Deploy

Este projeto é proprietário - © 2025 Solis PDV



## ✨ Learn More

# Aguardar inicialização (cria schemas automaticamente)### Vercel (Recomendado)

- [Next.js API Routes](https://nextjs.org/docs/app/building-your-application/routing/route-handlers)

- [PostgreSQL Multi-Tenancy](https://www.postgresql.org/docs/current/ddl-schemas.html)docker logs -f solis-postgres```bash

- [Database Connection Pooling](https://node-postgres.com/apis/pool)

```vercel deploy

```

### Subir API

Configurar variáveis de ambiente no painel da Vercel.

```bash

# Build e run### Docker

docker-compose up -d --build solis-api```bash

docker build -t solis-api .

# Logsdocker run -p 3000:3000 solis-api

docker logs -f solis-api```

```

## 📚 Documentação Adicional

## 🧪 Testes

- [Next.js Docs](https://nextjs.org/docs)

### Testar tenant com schema compartilhado- [PostgreSQL Multi-Tenancy](https://www.postgresql.org/docs/current/ddl-schemas.html)

```bash- [Middleware](https://nextjs.org/docs/app/building-your-application/routing/middleware)

curl "http://localhost:3000/api/health?tenant=demo"

# Deve retornar: "type": "SCHEMA"
```

### Testar tenant com banco dedicado
```bash
# 1. Criar banco dedicado
docker run -d \
  --name solis-enterprise \
  --network solis-pdv-network \
  -e POSTGRES_DB=enterprise_db \
  -e POSTGRES_USER=enterprise_user \
  -e POSTGRES_PASSWORD=enterprise_pass \
  -p 5433:5432 \
  postgres:15-alpine

# 2. Configurar .env.local
# DB_ENTERPRISE_URL=postgresql://enterprise_user:enterprise_pass@localhost:5433/enterprise_db

# 3. Testar
curl "http://localhost:3000/api/health?tenant=enterprise"

# Deve retornar: "type": "DATABASE"
```

## 📁 Estrutura do Projeto

```
solis-api/
├── app/
│   ├── api/
│   │   ├── health/
│   │   │   └── route.ts          # Health check endpoint
│   │   ├── produtos/              # (em desenvolvimento)
│   │   └── vendas/                # (em desenvolvimento)
│   ├── layout.tsx
│   └── page.tsx                   # Homepage
├── lib/
│   ├── database.ts                # 🔥 Connection pooling híbrido
│   └── tenant.ts                  # Utilitários de tenant
├── middleware.ts                  # 🔥 Extração de tenant
├── public/
├── .env.local                     # Variáveis de ambiente
├── HYBRID_ARCHITECTURE.md         # 📖 Documentação detalhada
├── next.config.ts
├── package.json
└── tsconfig.json
```

## 🔧 Migrations

```bash
# Aplicar migrations no banco compartilhado
psql -h localhost -U solis_user -d solis_pdv -f database/init/01-init-multitenant.sql

# Aplicar migrations em banco dedicado
psql -h db-enterprise.com -U enterprise_user -d enterprise_db -f database/init/01-init-multitenant.sql
```

## 🎯 Roadmap

- [x] Middleware de extração de tenant
- [x] Connection pooling por tenant
- [x] Suporte a schemas compartilhados
- [x] Suporte a bancos dedicados
- [x] Health check endpoint
- [ ] CRUD de Produtos
- [ ] CRUD de Vendas
- [ ] Autenticação JWT
- [ ] Rate limiting por tenant
- [ ] Métricas de uso
- [ ] Dashboard de gerenciamento
- [ ] Script de migração schema → database
- [ ] Backup/restore por tenant

## 📚 Documentação Adicional

- [Arquitetura Híbrida Detalhada](./HYBRID_ARCHITECTURE.md)
- [Exemplo de Tenant Dedicado](./.env.dedicated-tenant-example)

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 License

Este projeto é proprietário - © 2025 Solis PDV

## ✨ Learn More

- [Next.js Documentation](https://nextjs.org/docs)
- [PostgreSQL Multi-Tenancy](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [Database Connection Pooling](https://node-postgres.com/apis/pool)
