# Solis API - Test Suite

Scripts automatizados para testar todos os endpoints da API.

## 🚀 Como usar

### PowerShell (Windows)

```powershell
# Executar com configurações padrão (localhost:5287, tenant demo)
.\tests\api-tests.ps1

# Executar com URL e tenant customizados
.\tests\api-tests.ps1 -ApiUrl "http://localhost:5000" -Tenant "acme"
```

### Bash (Linux/Mac)

```bash
# Tornar executável
chmod +x tests/api-tests.sh

# Executar com configurações padrão
./tests/api-tests.sh

# Executar com URL e tenant customizados
API_URL="http://localhost:5000" TENANT="acme" ./tests/api-tests.sh
```

## 📋 O que é testado

### 1. Health Check
- ✓ Endpoint `/api/health` responde

### 2. Authentication
- ✓ Login com credenciais válidas
- ✓ Login com credenciais inválidas (deve falhar)
- ✓ Token JWT gerado corretamente

### 3. Dynamic CRUD - User
- ✓ Get metadata
- ✓ List users com paginação
- ✓ Get user por ID
- ✓ Create new user
- ✓ Update user
- ✓ Delete user (soft delete)
- ✓ Search users

### 4. Dynamic CRUD - Tax Regime
- ✓ Get metadata
- ✓ List tax regimes

### 5. Dynamic CRUD - Special Tax Regime
- ✓ Get metadata
- ✓ List special tax regimes

### 6. Dynamic CRUD - Company
- ✓ Get metadata
- ✓ List companies
- ✓ Get company por ID
- ✓ Get options para relacionamentos

### 7. Field Options
- ✓ Get static options (role)
- ✓ Get dynamic options (relacionamentos)

### 8. Error Handling
- ✓ Entity não existente retorna 404
- ✓ ID não existente retorna 404
- ✓ Campos obrigatórios ausentes retornam 400
- ✓ Request sem token retorna 401

## 📊 Saída

Os scripts mostram:
- ✓ Testes que passaram (verde)
- ✗ Testes que falharam (vermelho)
- Detalhes dos erros (amarelo)
- Resumo final com contagem de sucessos/falhas

## 🔧 Pré-requisitos

### PowerShell
- PowerShell 5.1+ ou PowerShell Core 7+
- API rodando (ex: `dotnet run`)

### Bash
- Bash 4+
- `curl` instalado
- `jq` instalado (para parsing JSON)
  ```bash
  # Ubuntu/Debian
  sudo apt-get install jq
  
  # macOS
  brew install jq
  ```

## 🎯 CI/CD

Exemplo de uso em pipeline:

```yaml
# GitHub Actions
- name: Run API Tests
  run: |
    dotnet run --project SolisApi.csproj &
    sleep 10
    pwsh tests/api-tests.ps1

# GitLab CI
test:
  script:
    - dotnet run --project SolisApi.csproj &
    - sleep 10
    - bash tests/api-tests.sh
```

## 📝 Notas

- Os scripts aguardam a API estar rodando
- Testes criam dados temporários (usuários) que são deletados automaticamente
- Use tenant/ambiente separado para testes se necessário
- Exit code: 0 = sucesso, 1 = falha (útil para CI/CD)
