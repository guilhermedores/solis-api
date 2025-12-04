# =============================================
# Script: execute-sql.ps1
# Description: Execute SQL file using Npgsql
# =============================================

param(
    [Parameter(Mandatory=$true)]
    [string]$SqlFile,
    
    [string]$DbHost = "localhost",
    [string]$DbPort = "5432",
    [string]$Database = "solis_pdv",
    [string]$Username = "solis_user",
    [string]$Password = "solis2024"
)

$ErrorActionPreference = "Stop"

Write-Host "Executando SQL: $SqlFile" -ForegroundColor Cyan

# Read SQL file
if (-not (Test-Path $SqlFile)) {
    Write-Host "Arquivo não encontrado: $SqlFile" -ForegroundColor Red
    exit 1
}

$sql = Get-Content $SqlFile -Raw

# Connection string
$connectionString = "Host=$DbHost;Port=$DbPort;Database=$Database;Username=$Username;Password=$Password"

# Execute SQL using .NET
Add-Type -Path "C:\Users\Guilherme Batista\solis-repos\solis-api\bin\Debug\net10.0\Npgsql.dll"

try {
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Conexão estabelecida" -ForegroundColor Green
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 300
    
    Write-Host "Executando script..." -ForegroundColor Yellow
    $result = $command.ExecuteNonQuery()
    
    Write-Host "✓ Script executado com sucesso!" -ForegroundColor Green
    Write-Host "  Linhas afetadas: $result" -ForegroundColor Gray
    
    $connection.Close()
}
catch {
    Write-Host "✗ Erro ao executar script:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($connection) { $connection.Close() }
    exit 1
}
