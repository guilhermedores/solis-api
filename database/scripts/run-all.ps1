# =============================================
# Script: run-all.ps1
# Description: Executes all SQL scripts in order
# Author: Solis Team
# Date: 2025-11-30
# =============================================

param(
    [string]$DbHost = "localhost",
    [string]$DbPort = "5432",
    [string]$Database = "solis_pdv",
    [string]$Username = "solis_user",
    [string]$Password = "solis2024",
    [switch]$UseDocker = $false,
    [switch]$SkipSeed = $false
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }

Write-Info "=========================================="
Write-Info "  Solis API - Database Setup Script"
Write-Info "=========================================="
Write-Info ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Get SQL files (exclude 99-rollback)
$SqlFiles = Get-ChildItem "$ScriptDir\*.sql" | 
    Where-Object { $_.Name -notlike "99-*" } | 
    Sort-Object Name

if ($SkipSeed) {
    $SqlFiles = $SqlFiles | Where-Object { $_.Name -notlike "04-seed-*" }
    Write-Warning "Skipping seed scripts (04-*)"
}

Write-Info "Found $($SqlFiles.Count) SQL scripts to execute"
Write-Info ""

# Execute each script
$ExecutedCount = 0
$FailedCount = 0

foreach ($SqlFile in $SqlFiles) {
    Write-Info "[$($ExecutedCount + 1)/$($SqlFiles.Count)] Executing: $($SqlFile.Name)"
    
    try {
        if ($UseDocker) {
            # Execute via Docker
            Get-Content $SqlFile.FullName | docker exec -i solis-postgres psql -U $Username -d $Database 2>&1 | Out-Null
        }
        else {
            # Execute via psql (requires psql in PATH)
            $env:PGPASSWORD = $Password
            Get-Content $SqlFile.FullName | psql -h $DbHost -p $DbPort -U $Username -d $Database -q 2>&1 | Out-Null
            Remove-Item Env:\PGPASSWORD
        }
        
        Write-Success "  ✓ Success"
        $ExecutedCount++
    }
    catch {
        Write-Error "  ✗ Failed: $($_.Exception.Message)"
        $FailedCount++
    }
    
    Write-Info ""
}

# Summary
Write-Info "=========================================="
Write-Info "  Execution Summary"
Write-Info "=========================================="
Write-Success "Executed: $ExecutedCount scripts"

if ($FailedCount -gt 0) {
    Write-Error "Failed: $FailedCount scripts"
    exit 1
}
else {
    Write-Success "All scripts executed successfully!"
    Write-Info ""
    Write-Info "Demo tenant credentials:"
    Write-Info "  Admin:    admin@demo.com / Admin@123"
    Write-Info "  Manager:  manager@demo.com / Manager@123"
    Write-Info "  Operator: operator@demo.com / Operator@123"
    exit 0
}
