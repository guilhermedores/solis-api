# Docker Setup - Solis API

## 🐳 Início Rápido

### Subir a aplicação completa (PostgreSQL + API)

```bash
docker-compose up -d
```

### Verificar status dos containers

```bash
docker-compose ps
```

### Ver logs

```bash
# Logs da API
docker-compose logs -f api

# Logs do PostgreSQL
docker-compose logs -f postgres

# Todos os logs
docker-compose logs -f
```

### Parar os serviços

```bash
docker-compose down
```

### Rebuild da API (após mudanças no código)

```bash
docker-compose up -d --build api
```

## 📋 Serviços Disponíveis

### API - Solis API
- **URL**: http://localhost:5287
- **Health Check**: http://localhost:5287/api/health
- **Swagger**: http://localhost:5287/docs

### PostgreSQL
- **Host**: localhost
- **Porta**: 5432
- **Database**: solis_pdv
- **Usuário**: solis_user
- **Senha**: solis2024

## 🔧 Configuração

As configurações da API estão no `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=solis_pdv;...
  - Jwt__SecretKey=your-super-secret-key-change-this-in-production-min-32-chars
```

### Alterar a porta da API

No `docker-compose.yml`, altere:
```yaml
ports:
  - "5287:8080"  # Mude 5287 para a porta desejada
```

## 🧪 Testando a API

### Health Check
```powershell
Invoke-WebRequest http://localhost:5287/api/health
```

### Login
```powershell
$body = @{email="admin@demo.com";password="Admin@123"} | ConvertTo-Json
$headers = @{"X-Tenant-Subdomain"="demo";"Content-Type"="application/json"}
Invoke-RestMethod -Uri "http://localhost:5287/api/auth/login" -Method POST -Body $body -Headers $headers
```

## 🗄️ Dados Iniciais

O PostgreSQL é inicializado automaticamente com:
- Schema multi-tenant (tenant_demo)
- Usuário admin padrão
- Entidades do sistema (user, company, tax_regime, etc.)
- Dados de exemplo

Os scripts de inicialização estão em `./database/init/`

## 🐛 Troubleshooting

### API não inicia
```bash
# Ver logs detalhados
docker-compose logs api

# Reiniciar o container
docker-compose restart api
```

### PostgreSQL não está pronto
```bash
# Verificar health check
docker-compose ps

# Ver logs do PostgreSQL
docker-compose logs postgres
```

### Limpar tudo e reiniciar do zero
```bash
# Para e remove containers, networks e volumes
docker-compose down -v

# Rebuild completo
docker-compose up -d --build
```

## 📝 Desenvolvimento

### Modo desenvolvimento local
Se quiser desenvolver localmente mas usar o PostgreSQL do Docker:

```bash
# Apenas o PostgreSQL
docker-compose up -d postgres

# Rodar a API localmente
dotnet run
```

### Hot reload durante desenvolvimento
Para desenvolvimento com hot reload, é melhor rodar localmente:

```bash
# PostgreSQL no Docker
docker-compose up -d postgres

# API local com hot reload
dotnet watch run
```

## 🔐 Segurança

**⚠️ IMPORTANTE**: Antes de ir para produção:

1. Altere a senha do PostgreSQL em `docker-compose.yml`
2. Altere o `Jwt__SecretKey` para um valor seguro
3. Use variáveis de ambiente ao invés de hardcode
4. Configure HTTPS
5. Revise as portas expostas

## 🌐 Frontend

A API estará disponível em `http://localhost:5287` para seu frontend consumir.

Exemplo de configuração no frontend:
```javascript
const API_BASE_URL = 'http://localhost:5287/api';
```
