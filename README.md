# Solis API - ASP.NET Core 9.0

API REST para gerenciamento de PDV (Ponto de Venda) com arquitetura multi-tenant baseada em schemas do PostgreSQL.

## 🚀 Tecnologias

- **ASP.NET Core 9.0** - Framework web
- **Entity Framework Core 9.0** - ORM
- **PostgreSQL** - Banco de dados multi-tenant
- **JWT Authentication** - Autenticação baseada em tokens
- **BCrypt** - Hash de senhas
- **Swagger/OpenAPI** - Documentação interativa da API

## 📁 Estrutura do Projeto

```
solis-api/
├── Controllers/          # Endpoints da API
│   ├── AuthController.cs
│   └── UsuariosController.cs
├── Data/                 # DbContext e acesso a dados
│   ├── SolisDbContext.cs
│   └── TenantDbContext.cs
├── DTOs/                 # Data Transfer Objects
│   └── AuthDTOs.cs
├── Middleware/           # Middlewares customizados
│   ├── JwtAuthMiddleware.cs
│   └── AuthorizationAttributes.cs
├── Models/               # Modelos de domínio
│   ├── Tenant.cs
│   ├── Usuario.cs
│   └── Empresa.cs
├── Services/             # Lógica de negócio
│   ├── AuthService.cs
│   └── UserService.cs
├── Properties/
│   └── launchSettings.json
├── Program.cs            # Configuração e startup
├── SolisApi.csproj       # Arquivo do projeto
├── appsettings.json      # Configurações
└── appsettings.Development.json
```

## 🗄️ Arquitetura Multi-Tenant

O sistema utiliza **schema-based isolation** no PostgreSQL:

- **Schema `public`**: Armazena a tabela `tenants` (gerenciamento de clientes)
- **Schemas `tenant_*`**: Cada tenant tem seu próprio schema com tabelas:
  - `users` - Usuários do tenant
  - `empresas` - Empresas/estabelecimentos
  - Outras tabelas de negócio

### Exemplo:
```
public.tenants          → Todos os tenants
tenant_demo.users       → Usuários do tenant "demo"
tenant_demo.empresas    → Empresas do tenant "demo"
tenant_cliente1.users   → Usuários do tenant "cliente1"
tenant_cliente1.empresas → Empresas do tenant "cliente1"
```

## 🔐 Autenticação e Autorização

### JWT Token Structure
```json
{
  "userId": "uuid",
  "empresaId": "uuid",
  "tenantId": "uuid",
  "tenant": "subdomain",
  "role": "admin|manager|operator",
  "type": "user|agent"
}
```

### Roles
- **admin**: Acesso total ao sistema
- **manager**: Gerenciamento de usuários e configurações
- **operator**: Acesso limitado às operações básicas

### Atributos de Autorização
- `[RequireAuth]` - Exige autenticação
- `[RequireRole("admin", "manager")]` - Exige roles específicas
- `[RequireAdmin]` - Apenas administradores
- `[RequireManager]` - Administradores ou gerentes

## ⚙️ Configuração

### 1. Banco de Dados

Configure a connection string em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=solis_pdv;Username=solis_user;Password=sua_senha"
  }
}
```

### 2. JWT Secret

Configure o secret para JWT em `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters-long-for-security",
    "Issuer": "SolisApi",
    "Audience": "SolisApi"
  }
}
```

⚠️ **IMPORTANTE**: Altere o secret em produção!

## 🏃 Como Executar

### Pré-requisitos
- .NET 9.0 SDK
- PostgreSQL 12+
- Banco de dados `solis_pdv` criado

### Executar em Desenvolvimento

```powershell
# Compilar o projeto
dotnet build

# Executar a API
dotnet run
```

A API estará disponível em:
- **HTTP**: http://localhost:5287
- **Swagger**: http://localhost:5287/docs

### Executar com Watch (Hot Reload)

```powershell
dotnet watch run
```

## 📚 Endpoints da API

### Health Check
```http
GET /api/health
```

### Autenticação

#### Login
```http
POST /api/auth/login?tenantSubdomain=demo
Content-Type: application/json

{
  "email": "admin@demo.com",
  "password": "senha123"
}
```

**Resposta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "usuario": {
    "id": "uuid",
    "nome": "Admin Demo",
    "email": "admin@demo.com",
    "role": "admin",
    "ativo": true
  }
}
```

### Usuários (Requer Autenticação)

#### Listar Usuários
```http
GET /api/usuarios
Authorization: Bearer {token}
```

#### Buscar Usuário por ID
```http
GET /api/usuarios/{id}
Authorization: Bearer {token}
```

#### Criar Usuário (Manager/Admin)
```http
POST /api/usuarios
Authorization: Bearer {token}
Content-Type: application/json

{
  "nome": "João Silva",
  "email": "joao@demo.com",
  "password": "senha123",
  "role": "operator"
}
```

#### Atualizar Usuário
```http
PUT /api/usuarios/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "nome": "João Silva Atualizado",
  "email": "joao.novo@demo.com"
}
```

#### Desativar Usuário (Manager/Admin)
```http
DELETE /api/usuarios/{id}
Authorization: Bearer {token}
```

#### Reativar Usuário (Manager/Admin)
```http
POST /api/usuarios/{id}/reactivate
Authorization: Bearer {token}
```

## 🧪 Testes

Consulte o arquivo [TESTES.md](TESTES.md) para exemplos detalhados de testes com PowerShell e Swagger.

## 🔧 Desenvolvimento

### Adicionar Nova Migration (se usar EF Migrations)

```powershell
dotnet ef migrations add NomeDaMigration
dotnet ef database update
```

### Limpar Build

```powershell
dotnet clean
Remove-Item -Recurse -Force bin, obj
```

### Publicar para Produção

```powershell
dotnet publish -c Release -o ./publish
```

## 📦 Dependências

- `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.2) - PostgreSQL provider
- `Microsoft.EntityFrameworkCore.Design` (9.0.0) - EF Core tools
- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.0) - JWT authentication
- `BCrypt.Net-Next` (4.0.3) - Password hashing
- `System.IdentityModel.Tokens.Jwt` (8.15.0) - JWT tokens
- `Swashbuckle.AspNetCore` (7.2.0) - Swagger/OpenAPI

## 🐛 Troubleshooting

### Erro: "Cannot connect to database"
- Verifique se o PostgreSQL está rodando
- Confirme as credenciais em `appsettings.json`
- Teste a conexão: `psql -h localhost -U solis_user -d solis_pdv`

### Erro: "Token inválido"
- Verifique se o header está correto: `Authorization: Bearer {token}`
- Confirme se o token não expirou (30 dias para usuários)
- Verifique se o JWT Secret está configurado corretamente

### Erro: "Tenant não encontrado"
- Verifique se o tenant existe na tabela `public.tenants`
- Confirme se o tenant está ativo (`active = true`)
- Verifique se o subdomain está correto

## 📝 Licença

Este projeto é proprietário e confidencial.

## 👥 Equipe

Desenvolvido por Solis Software
