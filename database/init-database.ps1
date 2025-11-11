# =============================================================================
# Script PowerShell para inicializar o banco de dados
# Execute: .\database\init-database.ps1
# =============================================================================

Write-Host "Iniciando configuracao do banco de dados..." -ForegroundColor Cyan
Write-Host ""

# Carregar variáveis de ambiente do .env
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*?)\s*=\s*(.*)$') {
            $key = $matches[1]
            $value = $matches[2] -replace '"',''
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
    }
    Write-Host "Variaveis carregadas do .env" -ForegroundColor Green
} else {
    Write-Host "Arquivo .env nao encontrado, usando valores padrao" -ForegroundColor Yellow
}

# Variáveis de conexão
$DB_HOST = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$DB_PORT = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$DB_NAME = if ($env:DB_NAME) { $env:DB_NAME } else { "solis_pdv" }
$DB_USER = if ($env:DB_USER) { $env:DB_USER } else { "solis_user" }
$env:PGPASSWORD = if ($env:DB_PASSWORD) { $env:DB_PASSWORD } else { "solis123_secure_password" }

Write-Host ""
Write-Host "Configuracao do banco:" -ForegroundColor Cyan
Write-Host "  Host: $DB_HOST"
Write-Host "  Port: $DB_PORT"
Write-Host "  Database: $DB_NAME"
Write-Host "  User: $DB_USER"
Write-Host ""

# Função para executar SQL
function Run-SQL {
    param($FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "Arquivo nao encontrado: $FilePath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Executando: $FilePath" -ForegroundColor Yellow
    
    $output = psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $FilePath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: $FilePath executado com sucesso" -ForegroundColor Green
    } else {
        Write-Host "ERRO ao executar $FilePath" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# Testar conexão
Write-Host "Testando conexao com o banco de dados..." -ForegroundColor Yellow
$testConnection = psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT 1;" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Nao foi possivel conectar ao banco de dados!" -ForegroundColor Red
    Write-Host $testConnection -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifique:" -ForegroundColor Yellow
    Write-Host "  - PostgreSQL esta rodando?"
    Write-Host "  - Credenciais estao corretas no .env?"
    Write-Host "  - Banco de dados '$DB_NAME' existe?"
    exit 1
}
Write-Host "Conexao bem-sucedida!" -ForegroundColor Green
Write-Host ""

# Executar scripts na ordem
Write-Host "Executando scripts de inicializacao..." -ForegroundColor Cyan
Write-Host ""

Run-SQL "database\init\01-create-database.sql"
Run-SQL "database\init\02-create-demo-tenant.sql"
Run-SQL "database\init\03-create-tenant-tables.sql"
Run-SQL "database\init\04-seed-demo-data.sql"

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "Banco de dados inicializado com sucesso!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Estrutura criada:" -ForegroundColor Cyan
Write-Host "  - Schema public: tabela 'Tenant'"
Write-Host "  - Schema tenant_demo: tabelas 'Usuario', 'Empresa'"
Write-Host "  - Tenant 'demo' criado e ativo"
Write-Host "  - Empresa demo criada"
Write-Host ""
Write-Host "Proximo passo: Criar usuario admin" -ForegroundColor Yellow
Write-Host "  Execute: npm run create-admin demo" -ForegroundColor White
Write-Host ""
