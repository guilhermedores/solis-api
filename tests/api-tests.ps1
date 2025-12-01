#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script de testes automatizados da Solis API
.DESCRIPTION
    Testa todos os endpoints principais: Auth, Dynamic CRUD (user, company, tax_regime, special_tax_regime)
.PARAMETER ApiUrl
    URL base da API (padrão: http://localhost:5287)
.PARAMETER Tenant
    Subdomain do tenant (padrão: demo)
.EXAMPLE
    .\api-tests.ps1
    .\api-tests.ps1 -ApiUrl "http://localhost:5000" -Tenant "acme"
#>

param(
    [string]$ApiUrl = "http://localhost:5287",
    [string]$Tenant = "demo"
)

$ErrorActionPreference = "Continue"
$Global:TestsPassed = 0
$Global:TestsFailed = 0
$Global:Token = ""

function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
    $Global:TestsPassed++
}

function Write-Failure {
    param([string]$Message, [string]$Details = "")
    Write-Host "✗ $Message" -ForegroundColor Red
    if ($Details) {
        Write-Host "  Details: $Details" -ForegroundColor Yellow
    }
    $Global:TestsFailed++
}

function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 200,
        [string]$TestName
    )
    
    try {
        $defaultHeaders = @{
            'X-Tenant-Subdomain' = $Tenant
            'Content-Type' = 'application/json'
        }
        
        if ($Global:Token) {
            $defaultHeaders['Authorization'] = "Bearer $Global:Token"
        }
        
        foreach ($key in $Headers.Keys) {
            $defaultHeaders[$key] = $Headers[$key]
        }
        
        $params = @{
            Uri = "$ApiUrl$Endpoint"
            Method = $Method
            Headers = $defaultHeaders
            ContentType = 'application/json'
        }
        
        if ($Body) {
            $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-RestMethod @params
        
        Write-Success $TestName
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq $ExpectedStatus) {
            Write-Success "$TestName (Expected $ExpectedStatus)"
            return $null
        }
        
        $errorBody = ""
        if ($_.ErrorDetails.Message) {
            $errorBody = $_.ErrorDetails.Message
        }
        
        Write-Failure $TestName "Status: $statusCode, Expected: $ExpectedStatus. Error: $errorBody"
        return $null
    }
}

# ====================
# INÍCIO DOS TESTES
# ====================

Write-Host @"

╔═══════════════════════════════════════╗
║      SOLIS API - TEST SUITE          ║
║                                       ║
║  API: $ApiUrl
║  Tenant: $Tenant
╚═══════════════════════════════════════╝

"@ -ForegroundColor Magenta

# ====================
# 1. HEALTH CHECK
# ====================

Write-TestHeader "1. Health Check"

Invoke-ApiTest -Method GET -Endpoint "/health" -TestName "Health endpoint responds"

# ====================
# 2. TENANT CHECK (PUBLIC)
# ====================

Write-TestHeader "2. Tenant Check Tests (Public - No Auth)"

# Remove token temporariamente para testar endpoint público
$savedToken = $Global:Token
$Global:Token = ""

# Verificar tenant existente e ativo
$tenantCheck = Invoke-ApiTest -Method GET -Endpoint "/api/tenants/check/$Tenant" `
    -TestName "Check existing tenant (demo)"

if ($tenantCheck) {
    Write-Host "  Tenant exists: $($tenantCheck.exists), Active: $($tenantCheck.active), Name: $($tenantCheck.tradeName)" -ForegroundColor Gray
}

# Verificar tenant inexistente
$nonExistentCheck = Invoke-ApiTest -Method GET -Endpoint "/api/tenants/check/nonexistent" `
    -TestName "Check non-existent tenant"

if ($nonExistentCheck) {
    Write-Host "  Tenant exists: $($nonExistentCheck.exists)" -ForegroundColor Gray
}

# Restaurar token
$Global:Token = $savedToken

# ====================
# 3. AUTHENTICATION
# ====================

Write-TestHeader "3. Authentication Tests"

# Login com admin
$loginResponse = Invoke-ApiTest -Method POST -Endpoint "/api/auth/login" `
    -Body @{email="admin@internal.com"; password="Admin@123"} `
    -TestName "Login with admin credentials"

if ($loginResponse -and $loginResponse.token) {
    $Global:Token = $loginResponse.token
    Write-Host "  Token: $($Global:Token.Substring(0, 50))..." -ForegroundColor Gray
}

# Login com credenciais inválidas (deve falhar)
Invoke-ApiTest -Method POST -Endpoint "/api/auth/login" `
    -Body @{email="admin@internal.com"; password="WrongPassword"} `
    -ExpectedStatus 401 `
    -TestName "Login with invalid password fails"

# ====================
# 4. ENTITIES LIST
# ====================

Write-TestHeader "4. Entities List"

$entitiesResponse = Invoke-ApiTest -Method GET -Endpoint "/api/entities" `
    -TestName "Get all entities with categories"

if ($entitiesResponse -and $entitiesResponse.entities) {
    Write-Host "  Found $($entitiesResponse.entities.Count) accessible entities" -ForegroundColor Gray
    $grouped = $entitiesResponse.entities | Group-Object -Property category
    foreach ($group in $grouped) {
        Write-Host "    $($group.Name): $($group.Count) entities" -ForegroundColor Gray
    }
}

# ====================
# 5. DYNAMIC CRUD - USER
# ====================

Write-TestHeader "5. Dynamic CRUD - User Entity"

# Get metadata
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user/_metadata" `
    -TestName "Get user metadata"

# List users
$users = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user?page=1&pageSize=10" `
    -TestName "List users with pagination"

if ($users -and $users.data) {
    Write-Host "  Found $($users.data.Count) users, Total: $($users.pagination.totalCount)" -ForegroundColor Gray
}

# Get first user by ID
if ($users -and $users.data -and $users.data.Count -gt 0) {
    $userId = $users.data[0].data.id
    if (-not $userId) {
        $userId = $users.data[0].id
    }
    Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user/$userId" `
        -TestName "Get user by ID"
}

# Create new user
$newUser = Invoke-ApiTest -Method POST -Endpoint "/api/dynamic/user" `
    -Body @{
        name = "Test User $(Get-Random -Maximum 1000)"
        email = "testuser$(Get-Random -Maximum 10000)@internal.com"
        password = "Test@123"
        role = "operator"
        active = $true
    } `
    -TestName "Create new user"

if ($newUser -and $newUser.id) {
    $createdUserId = $newUser.id
    Write-Host "  Created user ID: $createdUserId" -ForegroundColor Gray
    
    # Update user
    Invoke-ApiTest -Method PUT -Endpoint "/api/dynamic/user/$createdUserId" `
        -Body @{name = "Updated Test User"} `
        -ExpectedStatus 204 `
        -TestName "Update user"
    
    # Delete user
    Invoke-ApiTest -Method DELETE -Endpoint "/api/dynamic/user/$createdUserId" `
        -ExpectedStatus 204 `
        -TestName "Delete user (soft delete)"
}

# Search users
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user?search=admin" `
    -TestName "Search users by name/email"

# ====================
# 6. DYNAMIC CRUD - TAX REGIME
# ====================

Write-TestHeader "6. Dynamic CRUD - Tax Regime Entity"

Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/tax_regime/_metadata" `
    -TestName "Get tax_regime metadata"

$taxRegimes = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/tax_regime" `
    -TestName "List tax regimes"

if ($taxRegimes -and $taxRegimes.data) {
    Write-Host "  Found $($taxRegimes.data.Count) tax regimes" -ForegroundColor Gray
}

# ====================
# 7. DYNAMIC CRUD - SPECIAL TAX REGIME
# ====================

Write-TestHeader "7. Dynamic CRUD - Special Tax Regime Entity"

Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/special_tax_regime/_metadata" `
    -TestName "Get special_tax_regime metadata"

$specialTaxRegimes = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/special_tax_regime" `
    -TestName "List special tax regimes"

if ($specialTaxRegimes -and $specialTaxRegimes.data) {
    Write-Host "  Found $($specialTaxRegimes.data.Count) special tax regimes" -ForegroundColor Gray
}

# ====================
# 8. DYNAMIC CRUD - COMPANY
# ====================

Write-TestHeader "8. Dynamic CRUD - Company Entity"

Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/company/_metadata" `
    -TestName "Get company metadata"

$companies = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/company" `
    -TestName "List companies"

if ($companies -and $companies.data) {
    Write-Host "  Found $($companies.data.Count) companies" -ForegroundColor Gray
}

# Get company by ID
if ($companies -and $companies.data -and $companies.data.Count -gt 0) {
    $companyId = $companies.data[0].data.id
    if (-not $companyId) {
        $companyId = $companies.data[0].id
    }
    Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/company/$companyId" `
        -TestName "Get company by ID"
}

# Get options for tax_regime field
if ($companies -and $companies.data -and $companies.data.Count -gt 0) {
    $companyId = $companies.data[0].data.id
    if (-not $companyId) {
        $companyId = $companies.data[0].id
    }
    Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/company/$companyId/options/tax_regime_id" `
        -TestName "Get tax regime options for company"
}

# ====================
# 9. DYNAMIC CRUD - PRODUCTS MODULE
# ====================

Write-TestHeader "9. Dynamic CRUD - Products Module"

# Product Groups
$productGroups = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/product_group" `
    -TestName "List product groups"

if ($productGroups -and $productGroups.data) {
    Write-Host "  Found $($productGroups.data.Count) product groups" -ForegroundColor Gray
}

# Product Subgroups
$productSubgroups = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/product_subgroup" `
    -TestName "List product subgroups"

if ($productSubgroups -and $productSubgroups.data) {
    Write-Host "  Found $($productSubgroups.data.Count) product subgroups" -ForegroundColor Gray
}

# Brands
$brands = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/brand" `
    -TestName "List brands"

if ($brands -and $brands.data) {
    Write-Host "  Found $($brands.data.Count) brands" -ForegroundColor Gray
}

# Products
$products = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/product" `
    -TestName "List products"

if ($products -and $products.data) {
    Write-Host "  Found $($products.data.Count) products" -ForegroundColor Gray
}

# Product metadata
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/product/_metadata" `
    -TestName "Get product metadata"

# ====================
# 10. FIELD OPTIONS
# ====================

Write-TestHeader "10. Field Options Tests"

if ($users -and $users.data -and $users.data.Count -gt 0) {
    $userId = $users.data[0].data.id
    if (-not $userId) {
        $userId = $users.data[0].id
    }
    $roleOptions = Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user/$userId/options/role" `
        -TestName "Get role options (static)"
    
    if ($roleOptions) {
        Write-Host "  Available roles: $($roleOptions.Count) options" -ForegroundColor Gray
    }
}

# ====================
# 8. ERROR HANDLING
# ====================

Write-TestHeader "8. Error Handling Tests"

# Entity não existe
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/nonexistent" `
    -ExpectedStatus 404 `
    -TestName "Non-existent entity returns 404"

# ID não existe
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user/00000000-0000-0000-0000-000000000000" `
    -ExpectedStatus 404 `
    -TestName "Non-existent user ID returns 404"

# Criar user sem campos obrigatórios
Invoke-ApiTest -Method POST -Endpoint "/api/dynamic/user" `
    -Body @{name = "Incomplete User"} `
    -ExpectedStatus 400 `
    -TestName "Create user without required fields fails"

# Request sem token (deve falhar em endpoints autenticados)
$Global:Token = ""
Invoke-ApiTest -Method GET -Endpoint "/api/dynamic/user" `
    -ExpectedStatus 401 `
    -TestName "Request without token fails"

# ====================
# SUMMARY
# ====================

Write-Host "`n" -NoNewline
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "           TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Tests: " -NoNewline
Write-Host ($Global:TestsPassed + $Global:TestsFailed) -ForegroundColor White
Write-Host "Passed: " -NoNewline
Write-Host $Global:TestsPassed -ForegroundColor Green
Write-Host "Failed: " -NoNewline
Write-Host $Global:TestsFailed -ForegroundColor Red

if ($Global:TestsFailed -eq 0) {
    Write-Host "`n[PASS] ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n[FAIL] SOME TESTS FAILED" -ForegroundColor Red
    exit 1
}
