# Dynamic CRUD System

## Overview

O sistema de CRUD dinâmico permite criar e gerenciar entidades através de metadados, sem necessidade de criar controllers e services individuais para cada entidade.

## Arquitetura

### Tabelas de Metadados (dentro de cada schema tenant)

1. **entities** - Define as entidades disponíveis
2. **entity_fields** - Define os campos de cada entidade
3. **entity_relationships** - Define relacionamentos entre entidades
4. **entity_field_options** - Define opções para campos select/multiselect
5. **entity_permissions** - Define permissões por role (admin, manager, operator)

### Como Funciona

1. Cada tenant tem suas próprias tabelas de metadados no schema `tenant_{subdomain}`
2. O `DynamicCrudService` lê os metadados e gera queries SQL dinamicamente
3. O `DynamicCrudController` expõe endpoints genéricos para qualquer entidade
4. As permissões são verificadas automaticamente baseadas no role do usuário

## Endpoints da API Dinâmica

### Base URL
```
/api/dynamic/{entityName}
```

### Endpoints Disponíveis

#### 1. Get Metadata
```
GET /api/dynamic/{entityName}/_metadata
```
Retorna os metadados da entidade (campos, tipos, validações, permissões).

**Exemplo:**
```bash
curl -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     http://localhost:5287/api/dynamic/user/_metadata
```

**Response:**
```json
{
  "id": "e0000000-0000-0000-0000-000000000001",
  "name": "user",
  "displayName": "Users",
  "tableName": "users",
  "fields": [
    {
      "name": "name",
      "displayName": "Name",
      "dataType": "string",
      "fieldType": "text",
      "isRequired": true,
      "showInList": true,
      "showInCreate": true
    }
  ],
  "permissions": [
    {
      "role": "admin",
      "canCreate": true,
      "canRead": true,
      "canUpdate": true,
      "canDelete": true
    }
  ]
}
```

#### 2. List Entities
```
GET /api/dynamic/{entityName}?page=1&pageSize=20&search=term&orderBy=name&ascending=true
```
Lista registros com paginação, busca e ordenação.

**Exemplo:**
```bash
curl -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     "http://localhost:5287/api/dynamic/user?page=1&pageSize=10&search=admin"
```

**Response:**
```json
{
  "data": [
    {
      "data": {
        "id": "...",
        "name": "Demo Admin",
        "email": "admin@demo.com",
        "role": "admin",
        "active": true,
        "created_at": "2025-11-30T00:00:00Z"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 3,
    "totalPages": 1
  }
}
```

#### 3. Get By ID
```
GET /api/dynamic/{entityName}/{id}
```
Busca um registro específico.

**Exemplo:**
```bash
curl -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     http://localhost:5287/api/dynamic/user/30000000-0000-0000-0000-000000000001
```

#### 4. Create
```
POST /api/dynamic/{entityName}
```
Cria um novo registro.

**Exemplo:**
```bash
curl -X POST \
     -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "New User",
       "email": "newuser@demo.com",
       "password": "Password@123",
       "role": "operator"
     }' \
     http://localhost:5287/api/dynamic/user
```

**Response:**
```json
{
  "id": "generated-uuid"
}
```

#### 5. Update
```
PUT /api/dynamic/{entityName}/{id}
```
Atualiza um registro existente.

**Exemplo:**
```bash
curl -X PUT \
     -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "Updated Name",
       "active": false
     }' \
     http://localhost:5287/api/dynamic/user/30000000-0000-0000-0000-000000000001
```

**Response:** `204 No Content`

#### 6. Delete
```
DELETE /api/dynamic/{entityName}/{id}
```
Deleta um registro (soft delete se houver campo `active`).

**Exemplo:**
```bash
curl -X DELETE \
     -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     http://localhost:5287/api/dynamic/user/30000000-0000-0000-0000-000000000001
```

**Response:** `204 No Content`

#### 7. Get Field Options
```
GET /api/dynamic/{entityName}/{id}/options/{fieldName}
```
Retorna opções para campos select (estáticas ou dinâmicas via relacionamento).

**Exemplo:**
```bash
curl -H "X-Tenant-Subdomain: demo" \
     -H "Authorization: Bearer {token}" \
     http://localhost:5287/api/dynamic/company/new/options/tax_regime_id
```

**Response:**
```json
[
  {
    "value": "10000000-0000-0000-0000-000000000001",
    "label": "Simples Nacional"
  },
  {
    "value": "10000000-0000-0000-0000-000000000002",
    "label": "Lucro Presumido"
  }
]
```

## Entidades Disponíveis

### 1. user
- **Table:** `users`
- **Permissões:**
  - Admin: CRUD completo
  - Manager: Criar, ler, atualizar (não deleta)
  - Operator: Apenas ler próprios registros

### 2. company
- **Table:** `companies`
- **Permissões:**
  - Admin: CRUD completo
  - Manager: Criar, ler, atualizar (não deleta)
  - Operator: Apenas ler

### 3. tax_regime
- **Table:** `tax_regimes`
- **Permissões:**
  - Admin: CRUD completo
  - Manager/Operator: Apenas ler

### 4. special_tax_regime
- **Table:** `special_tax_regimes`
- **Permissões:**
  - Admin: CRUD completo
  - Manager/Operator: Apenas ler

## Como Adicionar Nova Entidade

### 1. Criar a tabela no schema do tenant

Edite `database/scripts/03-create-tenant-schema-function.sql` e adicione a criação da nova tabela dentro da função `create_tenant_schema`.

### 2. Adicionar metadados no seed

Edite `database/scripts/04-seed-demo-tenant.sql` e adicione:

```sql
-- Insert entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, icon, description)
VALUES (
    'unique-uuid',
    'product',
    'Products',
    'products',
    'box',
    'Product catalog'
);

-- Insert fields
INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, show_in_list, list_order)
VALUES
    ('unique-uuid', 'id', 'ID', 'id', 'uuid', 'text', true, false, 0),
    ('unique-uuid', 'name', 'Name', 'name', 'string', 'text', true, true, 1),
    ('unique-uuid', 'price', 'Price', 'price', 'number', 'number', true, true, 2),
    ('unique-uuid', 'active', 'Active', 'active', 'boolean', 'checkbox', true, true, 3);

-- Insert permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('unique-uuid', 'admin', true, true, true, true),
    ('unique-uuid', 'manager', true, true, true, false),
    ('unique-uuid', 'operator', false, true, false, false);
```

### 3. Recriar o banco

```powershell
.\database\scripts\run-all.ps1 -UseDocker
```

### 4. Usar a API

Pronto! A nova entidade já está disponível em:
- `GET /api/dynamic/product`
- `POST /api/dynamic/product`
- etc.

## Tipos de Campos Suportados

### Data Types
- `string` - Texto
- `number` - Números inteiros ou decimais
- `boolean` - Verdadeiro/Falso
- `date` - Data
- `datetime` - Data e hora
- `uuid` - Identificador único
- `json` - JSON estruturado
- `text` - Texto longo

### Field Types (para UI)
- `text` - Input de texto simples
- `textarea` - Área de texto multilinha
- `number` - Input numérico
- `email` - Input de email
- `password` - Input de senha
- `select` - Dropdown com opções
- `multiselect` - Dropdown múltiplo
- `date` - Seletor de data
- `datetime` - Seletor de data e hora
- `checkbox` - Checkbox
- `file` - Upload de arquivo
- `image` - Upload de imagem

## Validações

As validações são aplicadas automaticamente:
- `isRequired` - Campo obrigatório (validado no Create)
- `isUnique` - Valor único na tabela
- `maxLength` - Tamanho máximo
- `validationRegex` - Regex customizado
- `validationMessage` - Mensagem de erro customizada

## Segurança

- Todas as operações verificam permissões por role
- Queries SQL são parametrizadas (proteção contra SQL injection)
- Schema isolation garante separação entre tenants
- Campos system (`id`, `created_at`, `updated_at`) não podem ser alterados

## Performance

- Índices automáticos em:
  - Foreign keys
  - Campos únicos
  - Campos de busca frequente
- Paginação padrão (20 registros)
- Search otimizado com ILIKE
- Caching de metadados recomendado (TODO)

## Controllers Antigos (DEPRECADOS)

Os seguintes controllers ainda existem mas devem ser migrados para o sistema dinâmico:
- `UsersController` → Use `/api/dynamic/user`
- `CompaniesController` → Use `/api/dynamic/company`

**TODO:** Remover esses controllers após migração completa dos clientes.
