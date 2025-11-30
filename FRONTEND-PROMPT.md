# Prompt para Implementação do Dynamic CRUD no Frontend

## 📋 Contexto

Você precisa criar uma interface de administração dinâmica que consome uma API de CRUD genérico. A API já está pronta e rodando em `http://localhost:5287`.

## 🎯 Objetivo

Criar um sistema de **Dynamic CRUD** que:
1. Lista todas as entidades disponíveis no sistema
2. Para cada entidade, gera automaticamente telas de listagem, criação, edição e visualização
3. Renderiza formulários dinamicamente baseado nos metadados da API
4. Suporta relacionamentos entre entidades (dropdowns dinâmicos)
5. Implementa busca, filtros e paginação

## 🔌 API Base

**Base URL**: `http://localhost:5287/api`

### Autenticação

Todas as requisições (exceto login) precisam de:
```http
Authorization: Bearer {token}
X-Tenant-Subdomain: demo
Content-Type: application/json
```

**Login**:
```http
POST /api/auth/login
Content-Type: application/json
X-Tenant-Subdomain: demo

{
  "email": "admin@demo.com",
  "password": "Admin@123"
}

Response:
{
  "success": true,
  "token": "eyJhbGci...",
  "user": {
    "id": "...",
    "name": "Admin",
    "email": "admin@demo.com",
    "role": "admin"
  }
}
```

## 📊 Endpoints do Dynamic CRUD

### 1. Listar Entidades Disponíveis

```http
GET /api/entities
```

**Response**:
```json
{
  "entities": [
    {
      "name": "user",
      "displayName": "Users",
      "icon": "users",
      "description": "User management",
      "allowCreate": true,
      "allowRead": true,
      "allowUpdate": true,
      "allowDelete": true
    },
    {
      "name": "company",
      "displayName": "Companies",
      "icon": "building",
      "description": "Company management",
      "allowCreate": true,
      "allowRead": true,
      "allowUpdate": true,
      "allowDelete": true
    }
  ]
}
```

### 2. Obter Metadados de uma Entidade

```http
GET /api/dynamic/{entity}/_metadata
```

**Exemplo**: `GET /api/dynamic/user/_metadata`

**Response**:
```json
{
  "id": "...",
  "name": "user",
  "displayName": "Users",
  "tableName": "users",
  "icon": "users",
  "description": "User management",
  "fields": [
    {
      "id": "...",
      "name": "id",
      "displayName": "ID",
      "dataType": "uuid",
      "isRequired": true,
      "isReadOnly": true,
      "showInList": false,
      "showInForm": false,
      "showInDetail": true,
      "listOrder": 0,
      "formOrder": 0
    },
    {
      "id": "...",
      "name": "name",
      "displayName": "Name",
      "dataType": "string",
      "isRequired": true,
      "isReadOnly": false,
      "showInList": true,
      "showInForm": true,
      "showInDetail": true,
      "listOrder": 1,
      "formOrder": 1,
      "maxLength": 100,
      "validation": {
        "required": true,
        "minLength": 3,
        "maxLength": 100
      }
    },
    {
      "id": "...",
      "name": "email",
      "displayName": "Email",
      "dataType": "string",
      "isRequired": true,
      "showInList": true,
      "showInForm": true,
      "listOrder": 2,
      "formOrder": 2,
      "validation": {
        "required": true,
        "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
      }
    },
    {
      "id": "...",
      "name": "role",
      "displayName": "Role",
      "dataType": "string",
      "isRequired": true,
      "showInList": true,
      "showInForm": true,
      "listOrder": 3,
      "formOrder": 3,
      "hasOptions": true,
      "options": [
        {"value": "admin", "label": "Administrator"},
        {"value": "manager", "label": "Manager"},
        {"value": "operator", "label": "Operator"}
      ]
    },
    {
      "id": "...",
      "name": "active",
      "displayName": "Active",
      "dataType": "boolean",
      "isRequired": false,
      "showInList": true,
      "showInForm": true,
      "listOrder": 4,
      "formOrder": 4,
      "defaultValue": true
    }
  ],
  "relationships": [
    {
      "id": "...",
      "fieldId": "...",
      "relatedEntityName": "company",
      "relatedEntityDisplayName": "Company",
      "relationshipType": "many-to-one",
      "displayField": "trade_name",
      "foreignKeyColumn": "company_id"
    }
  ],
  "allowCreate": true,
  "allowRead": true,
  "allowUpdate": true,
  "allowDelete": true
}
```

### 3. Listar Registros

```http
GET /api/dynamic/{entity}?page=1&pageSize=20&search=termo&orderBy=name&ascending=true
```

**Exemplo**: `GET /api/dynamic/user?page=1&pageSize=20`

**Response**:
```json
{
  "data": [
    {
      "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "John Doe",
        "email": "john@example.com",
        "role": "admin",
        "active": true,
        "created_at": "2025-11-30T12:00:00Z"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

**Nota**: Acesse os dados como `response.data[0].data.id` (estrutura aninhada).

### 4. Obter Registro por ID

```http
GET /api/dynamic/{entity}/{id}
```

**Exemplo**: `GET /api/dynamic/user/550e8400-e29b-41d4-a716-446655440000`

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "John Doe",
  "email": "john@example.com",
  "role": "admin",
  "active": true,
  "created_at": "2025-11-30T12:00:00Z",
  "updated_at": "2025-11-30T13:00:00Z"
}
```

### 5. Criar Registro

```http
POST /api/dynamic/{entity}
Content-Type: application/json

{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "password": "SecurePass123",
  "role": "manager",
  "active": true
}
```

**Response**: 201 Created
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "name": "Jane Doe",
  "email": "jane@example.com",
  "role": "manager",
  "active": true,
  "created_at": "2025-11-30T14:00:00Z"
}
```

### 6. Atualizar Registro

```http
PUT /api/dynamic/{entity}/{id}
Content-Type: application/json

{
  "name": "Jane Smith",
  "active": false
}
```

**Response**: 204 No Content

### 7. Deletar Registro (Soft Delete)

```http
DELETE /api/dynamic/{entity}/{id}
```

**Response**: 204 No Content

### 8. Obter Opções para Campo (Dropdowns Dinâmicos)

Para campos com relacionamentos ou opções estáticas:

```http
GET /api/dynamic/{entity}/{id}/options/{fieldName}
```

**Exemplo 1 - Opções estáticas** (role):
```http
GET /api/dynamic/user/550e8400-e29b-41d4-a716-446655440000/options/role
```

**Response**:
```json
[
  {
    "id": "...",
    "value": "admin",
    "label": "Administrator",
    "description": "Full system access"
  },
  {
    "id": "...",
    "value": "manager",
    "label": "Manager",
    "description": "Manage operations"
  },
  {
    "id": "...",
    "value": "operator",
    "label": "Operator",
    "description": "Basic operations"
  }
]
```

**Exemplo 2 - Opções dinâmicas** (relacionamento com tax_regime):
```http
GET /api/dynamic/company/40000000-0000-0000-0000-000000000001/options/tax_regime_id
```

**Response**:
```json
[
  {
    "value": "10000000-0000-0000-0000-000000000001",
    "label": "Simples Nacional"
  },
  {
    "value": "10000000-0000-0000-0000-000000000002",
    "label": "Simples Nacional - Excesso de Sublimite"
  },
  {
    "value": "10000000-0000-0000-0000-000000000003",
    "label": "Regime Normal"
  }
]
```

## 🎨 Requisitos de Interface

### Estrutura Proposta

```
/admin
  /dashboard          (lista de entidades)
  /{entity}           (lista de registros)
  /{entity}/new       (formulário de criação)
  /{entity}/{id}      (visualização)
  /{entity}/{id}/edit (formulário de edição)
```

### Página de Dashboard

1. Buscar lista de entidades: `GET /api/dynamic`
2. Exibir cards/lista com:
   - Ícone da entidade
   - Nome (displayName)
   - Descrição
   - Link para listagem

### Página de Listagem

1. Buscar metadados: `GET /api/dynamic/{entity}/_metadata`
2. Buscar dados: `GET /api/dynamic/{entity}?page=1&pageSize=20`
3. Renderizar tabela com:
   - Colunas baseadas em `field.showInList = true`
   - Ordenação por `field.listOrder`
   - Busca global (search param)
   - Paginação
   - Botões de ação (ver, editar, deletar) baseados em permissões

### Página de Formulário

1. Buscar metadados: `GET /api/dynamic/{entity}/_metadata`
2. Renderizar campos dinamicamente:
   - Ordenar por `field.formOrder`
   - Filtrar por `field.showInForm = true`
   - Renderizar input apropriado por `dataType`:
     - `string` → `<input type="text">`
     - `integer`, `decimal` → `<input type="number">`
     - `boolean` → `<input type="checkbox">`
     - `date` → `<input type="date">`
     - `datetime` → `<input type="datetime-local">`
     - `uuid` → campo readonly (para IDs)
   - Para campos com `hasOptions = true` ou `hasRelationship = true`:
     - Buscar opções: `GET /api/dynamic/{entity}/{id}/options/{field}`
     - Renderizar `<select>` com as opções

3. Validações:
   - `isRequired` → campo obrigatório
   - `validation.minLength`, `maxLength` → validação de tamanho
   - `validation.pattern` → validação regex
   - `validation.min`, `max` → validação numérica

### Página de Visualização

1. Buscar metadados: `GET /api/dynamic/{entity}/_metadata`
2. Buscar registro: `GET /api/dynamic/{entity}/{id}`
3. Exibir campos readonly:
   - Filtrar por `field.showInDetail = true`
   - Formatar valores conforme dataType
   - Botões de ação (editar, deletar)

## 🔧 Tipos TypeScript Sugeridos

```typescript
interface Entity {
  name: string;
  displayName: string;
  icon: string;
  description: string;
  allowCreate: boolean;
  allowRead: boolean;
  allowUpdate: boolean;
  allowDelete: boolean;
}

interface EntityMetadata {
  id: string;
  name: string;
  displayName: string;
  tableName: string;
  icon: string;
  description: string;
  fields: Field[];
  relationships: Relationship[];
  allowCreate: boolean;
  allowRead: boolean;
  allowUpdate: boolean;
  allowDelete: boolean;
}

interface Field {
  id: string;
  name: string;
  displayName: string;
  dataType: 'string' | 'integer' | 'decimal' | 'boolean' | 'date' | 'datetime' | 'uuid' | 'text';
  isRequired: boolean;
  isReadOnly: boolean;
  showInList: boolean;
  showInForm: boolean;
  showInDetail: boolean;
  listOrder: number;
  formOrder: number;
  maxLength?: number;
  defaultValue?: any;
  hasOptions?: boolean;
  hasRelationship?: boolean;
  options?: FieldOption[];
  validation?: {
    required?: boolean;
    minLength?: number;
    maxLength?: number;
    min?: number;
    max?: number;
    pattern?: string;
  };
}

interface FieldOption {
  id?: string;
  value: string;
  label: string;
  description?: string;
}

interface Relationship {
  id: string;
  fieldId: string;
  relatedEntityName: string;
  relatedEntityDisplayName: string;
  relationshipType: 'one-to-one' | 'one-to-many' | 'many-to-one' | 'many-to-many';
  displayField: string;
  foreignKeyColumn: string;
}

interface ListResponse<T> {
  data: Array<{ data: T }>;
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}
```

## 🎯 Funcionalidades Essenciais

### ✅ DEVE ter:

1. **Menu lateral** com lista de entidades
2. **Listagem** com busca, ordenação e paginação
3. **Formulário dinâmico** que se adapta aos metadados
4. **Validação** baseada nas regras da API
5. **Dropdowns dinâmicos** para relacionamentos
6. **Feedback visual** (loading, success, error)
7. **Confirmação** antes de deletar
8. **Breadcrumbs** para navegação
9. **Mensagens de erro** da API

### 🎁 BÔNUS (se tiver tempo):

1. Filtros avançados por coluna
2. Exportar para CSV/Excel
3. Ações em lote (deletar múltiplos)
4. Drag & drop para reordenar
5. Upload de arquivos (se tiver campos file)
6. Visualização de relacionamentos (links clicáveis)
7. Histórico de alterações (audit log)
8. Favoritar entidades
9. Tema dark/light

## 📝 Exemplo de Fluxo Completo

### 1. Login
```typescript
const response = await fetch('http://localhost:5287/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-Tenant-Subdomain': 'demo'
  },
  body: JSON.stringify({
    email: 'admin@demo.com',
    password: 'Admin@123'
  })
});

const { token, user } = await response.json();
localStorage.setItem('authToken', token);
```

### 2. Buscar Entidades
```typescript
const response = await fetch('http://localhost:5287/api/entities', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo'
  }
});

const { entities } = await response.json();
// Renderizar menu com entidades
```

### 3. Listar Usuários
```typescript
const metadata = await fetch('http://localhost:5287/api/dynamic/user/_metadata', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo'
  }
}).then(r => r.json());

const users = await fetch('http://localhost:5287/api/dynamic/user?page=1&pageSize=20', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo'
  }
}).then(r => r.json());

// Renderizar tabela com metadata.fields e users.data
```

### 4. Criar Novo Usuário
```typescript
// Buscar opções para campo role
const roleOptions = await fetch(
  'http://localhost:5287/api/dynamic/user/00000000-0000-0000-0000-000000000000/options/role',
  {
    headers: {
      'Authorization': `Bearer ${token}`,
      'X-Tenant-Subdomain': 'demo'
    }
  }
).then(r => r.json());

// Renderizar formulário e submeter
const newUser = await fetch('http://localhost:5287/api/dynamic/user', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'X-Tenant-Subdomain': 'demo',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'Jane Doe',
    email: 'jane@example.com',
    password: 'SecurePass123',
    role: 'manager',
    active: true
  })
}).then(r => r.json());
```

## 🚨 Pontos de Atenção

1. **Estrutura de dados aninhada**: `response.data[0].data.id` (não `response.data[0].id`)
2. **Header obrigatório**: Sempre incluir `X-Tenant-Subdomain: demo`
3. **Campos password**: Nunca exibir em detalhes, apenas no formulário de criação
4. **Soft delete**: Ao deletar, o registro não é removido, apenas marcado como inativo
5. **UUIDs**: Usar o ID completo, não tentar criar IDs aleatórios
6. **Validação**: Validar no frontend antes de enviar para API
7. **Loading states**: Mostrar spinners durante requisições
8. **Error handling**: Tratar erros 400, 401, 404, 500 apropriadamente

## 🎨 Sugestões de UI/UX

### Framework/Bibliotecas sugeridas:
- **React** + React Router + React Hook Form
- **Tailwind CSS** ou **Material-UI** ou **Ant Design**
- **React Query** ou **SWR** para cache e refetch
- **Zod** ou **Yup** para validação
- **React Table** para tabelas avançadas

### Estrutura de componentes:
```
src/
  components/
    DynamicCrud/
      EntityList.tsx       (dashboard)
      EntityTable.tsx      (listagem)
      EntityForm.tsx       (formulário)
      EntityDetail.tsx     (visualização)
      DynamicField.tsx     (campo dinâmico)
      FieldRenderer.tsx    (renderiza campo por tipo)
  hooks/
    useEntityMetadata.ts
    useEntityData.ts
    useFieldOptions.ts
  services/
    api.ts               (client HTTP)
    auth.ts              (autenticação)
  types/
    entities.ts          (TypeScript types)
```

## ✅ Checklist de Implementação

- [ ] Configurar projeto e dependências
- [ ] Implementar autenticação (login + token storage)
- [ ] Criar layout base (header, sidebar, content)
- [ ] Implementar dashboard (lista de entidades)
- [ ] Criar componente de tabela dinâmica
- [ ] Implementar busca e paginação
- [ ] Criar formulário dinâmico
- [ ] Implementar validação de campos
- [ ] Adicionar suporte a dropdowns (options)
- [ ] Implementar criação de registros
- [ ] Implementar edição de registros
- [ ] Implementar exclusão com confirmação
- [ ] Adicionar feedback visual (toasts/alerts)
- [ ] Tratar erros da API
- [ ] Testar com todas as entidades (user, company, tax_regime, etc.)

## 🧪 Entidades Disponíveis para Teste

1. **user** - Usuários do sistema
2. **company** - Empresas
3. **tax_regime** - Regimes tributários
4. **special_tax_regime** - Regimes tributários especiais

Cada uma tem características diferentes (campos, validações, relacionamentos) para testar completamente o sistema.

---

**Boa sorte com a implementação! 🚀**

Se tiver dúvidas sobre algum endpoint ou comportamento da API, teste diretamente com curl/Postman ou consulte a documentação Swagger em `http://localhost:5287/docs`
