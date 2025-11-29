# Testes da API Solis - ASP.NET Core

## 🌐 Base URL
```
http://localhost:5287
```

## 📋 Endpoints de Teste

### 1. Health Check
Verificar se a API está funcionando:

```powershell
curl http://localhost:5287/api/health
```

**Resposta esperada**:
```json
{
  "status": "ok",
  "timestamp": "2025-11-29T...",
  "version": "1.0.0"
}
```

---

### 2. Login
Autenticar e obter token JWT:

```powershell
$body = @{
    email = "admin@demo.com"
    password = "senha123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5287/api/auth/login?tenantSubdomain=demo" -Method POST -Body $body -ContentType "application/json"

# Salvar token para próximas requisições
$token = $response.token
Write-Host "Token: $token"
```

**Resposta esperada**:
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

---

### 3. Listar Usuários (Requer autenticação)

```powershell
# Usando o token do login anterior
$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios" -Method GET -Headers $headers
```

**Resposta esperada**:
```json
[
  {
    "id": "uuid",
    "nome": "Admin Demo",
    "email": "admin@demo.com",
    "role": "admin",
    "ativo": true,
    "createdAt": "2025-11-29T...",
    "updatedAt": "2025-11-29T..."
  }
]
```

---

### 4. Criar Novo Usuário (Requer admin/manager)

```powershell
$headers = @{
    Authorization = "Bearer $token"
}

$body = @{
    nome = "João Silva"
    email = "joao@demo.com"
    password = "senha123"
    role = "operator"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios" -Method POST -Headers $headers -Body $body -ContentType "application/json"
```

---

### 5. Buscar Usuário por ID

```powershell
# Substituir {id} pelo UUID do usuário
$userId = "cole-o-id-aqui"

$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios/$userId" -Method GET -Headers $headers
```

---

### 6. Atualizar Usuário

```powershell
$userId = "cole-o-id-aqui"

$headers = @{
    Authorization = "Bearer $token"
}

$body = @{
    nome = "João Silva Atualizado"
    email = "joao.novo@demo.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios/$userId" -Method PUT -Headers $headers -Body $body -ContentType "application/json"
```

---

### 7. Desativar Usuário

```powershell
$userId = "cole-o-id-aqui"

$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios/$userId" -Method DELETE -Headers $headers
```

---

### 8. Reativar Usuário

```powershell
$userId = "cole-o-id-aqui"

$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5287/api/usuarios/$userId/reactivate" -Method POST -Headers $headers
```

---

## 🧪 Testar no Swagger

1. Abra no navegador: `http://localhost:5287/docs`
2. Clique em **POST /api/auth/login**
3. Clique em **Try it out**
4. Preencha:
   - Query Parameter `tenantSubdomain`: `demo`
   - Request body:
     ```json
     {
       "email": "admin@demo.com",
       "password": "senha123"
     }
     ```
5. Clique em **Execute**
6. Copie o token retornado
7. Clique no botão **Authorize** (cadeado no topo)
8. Cole o token (sem "Bearer") e clique em **Authorize**
9. Teste os outros endpoints

---

## ⚠️ Observações

- **Tenant**: O tenant "demo" deve existir no banco e estar ativo
- **Usuários de teste**: Devem ser criados previamente no schema `tenant_demo`
- **Token**: Válido por 30 dias para usuários normais
- **Roles**:
  - `admin`: Acesso total
  - `manager`: Gerenciar usuários
  - `operator`: Apenas próprios dados

---

## 🔍 Troubleshooting

### Erro: "Credenciais inválidas ou tenant não encontrado"
- Verifique se o tenant "demo" existe na tabela `public.tenants`
- Verifique se há usuários no schema `tenant_demo.users`
- Confirme a senha com bcrypt

### Erro: "Token não fornecido ou inválido"
- Adicione o header: `Authorization: Bearer {token}`
- Verifique se o token não expirou

### Erro de conexão com banco
- Confirme que o PostgreSQL está rodando
- Verifique as credenciais em `appsettings.json`
- Teste a conexão: `psql -h localhost -U solis_user -d solis_pdv`
