# ==================== Sales API Tests ====================
# Test script for Sales endpoints
# Usage: .\sales-tests.ps1 -BaseUrl "http://localhost:5287" -Token "your-jwt-token"

param(
    [string]$BaseUrl = "http://localhost:5287",
    [string]$Token = "",
    [string]$TenantId = ""
)

$ErrorActionPreference = "Continue"
$headers = @{
    "Content-Type" = "application/json"
}

if ($Token) {
    $headers["Authorization"] = "Bearer $Token"
}

if ($TenantId) {
    $headers["X-Tenant-ID"] = $TenantId
}

Write-Host "==================== SALES API TESTS ====================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Tenant ID: $TenantId" -ForegroundColor Yellow
Write-Host ""

# ==================== Helper Functions ====================

function Write-TestResult {
    param($TestName, $Success, $Response, $StatusCode)
    
    if ($Success) {
        Write-Host "✅ $TestName" -ForegroundColor Green
        Write-Host "   Status: $StatusCode" -ForegroundColor Gray
    } else {
        Write-Host "❌ $TestName" -ForegroundColor Red
        Write-Host "   Status: $StatusCode" -ForegroundColor Gray
    }
    
    if ($Response) {
        Write-Host "   Response: $($Response | ConvertTo-Json -Depth 2 -Compress)" -ForegroundColor Gray
    }
    Write-Host ""
}

# ==================== Test Data ====================

$storeId = "45000000-0000-0000-0000-000000000001"
$productId1 = "40000000-0000-0000-0000-000000000001"
$productId2 = "40000000-0000-0000-0000-000000000002"
$operatorId = "30000000-0000-0000-0000-000000000001"
$posId = "55555555-5555-5555-5555-555555555555"
$paymentMethodId = "60000000-0000-0000-0000-000000000001" # À Vista (Dinheiro)

# ==================== TEST 1: Create Sale (Pending) ====================

Write-Host "TEST 1: Create Sale (Pending - No Payments)" -ForegroundColor Cyan

$createSaleBody = @{
    storeId = $storeId
    posId = $posId
    operatorId = $operatorId
    saleDateTime = (Get-Date).ToUniversalTime().ToString("o")
    items = @(
        @{
            productId = $productId1
            quantity = 2
            unitPrice = 50.00
            discountAmount = 5.00
        },
        @{
            productId = $productId2
            quantity = 1
            unitPrice = 100.00
            discountAmount = 0
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales" -Method Post -Headers $headers -Body $createSaleBody
    $saleId = $response.id
    Write-TestResult "Create Sale (Pending)" $true $response 201
} catch {
    Write-TestResult "Create Sale (Pending)" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
    exit 1
}

# ==================== TEST 2: Get Sale by ID ====================

Write-Host "TEST 2: Get Sale by ID" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$saleId" -Method Get -Headers $headers
    Write-TestResult "Get Sale by ID" $true $response 200
} catch {
    Write-TestResult "Get Sale by ID" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 3: Get All Sales (List) ====================

Write-Host "TEST 3: Get All Sales (List with Pagination)" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales?page=1&pageSize=10" -Method Get -Headers $headers
    Write-TestResult "Get All Sales" $true $response 200
} catch {
    Write-TestResult "Get All Sales" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 4: Add Payment to Sale ====================

Write-Host "TEST 4: Add Payment to Sale" -ForegroundColor Cyan

$addPaymentBody = @{
    paymentMethodId = $paymentMethodId
    amount = 100.00
    acquirerTxnId = "TXN-12345"
    authorizationCode = "AUTH-67890"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$saleId/payments" -Method Post -Headers $headers -Body $addPaymentBody
    Write-TestResult "Add Payment (Partial)" $true $response 200
} catch {
    Write-TestResult "Add Payment (Partial)" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 5: Add Second Payment (Complete Sale) ====================

Write-Host "TEST 5: Add Second Payment to Complete Sale" -ForegroundColor Cyan

$addPayment2Body = @{
    paymentMethodId = $paymentMethodId
    amount = 95.00
    changeAmount = 5.00
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$saleId/payments" -Method Post -Headers $headers -Body $addPayment2Body
    Write-TestResult "Add Payment (Complete)" $true $response 200
    
    # Check if sale is completed
    $saleStatus = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$saleId" -Method Get -Headers $headers
    if ($saleStatus.status -eq "completed") {
        Write-Host "   ✅ Sale auto-completed when fully paid" -ForegroundColor Green
    }
} catch {
    Write-TestResult "Add Payment (Complete)" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 6: Create Sale with Payments (Direct Complete) ====================

Write-Host "TEST 6: Create Sale with Payments (Direct Complete)" -ForegroundColor Cyan

$createCompleteBody = @{
    storeId = $storeId
    posId = $posId
    operatorId = $operatorId
    saleDateTime = (Get-Date).ToUniversalTime().ToString("o")
    items = @(
        @{
            productId = $productId1
            quantity = 1
            unitPrice = 200.00
            discountAmount = 0
        }
    )
    payments = @(
        @{
            paymentMethodId = $paymentMethodId
            amount = 200.00
            acquirerTxnId = "PIX-98765"
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales" -Method Post -Headers $headers -Body $createCompleteBody
    $completeSaleId = $response.id
    Write-TestResult "Create Sale (Complete with Payments)" $true $response 201
    
    if ($response.status -eq "completed") {
        Write-Host "   ✅ Sale created as completed" -ForegroundColor Green
    }
} catch {
    Write-TestResult "Create Sale (Complete with Payments)" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 7: Cancel Sale ====================

Write-Host "TEST 7: Cancel Sale" -ForegroundColor Cyan

$cancelBody = @{
    reason = "Cliente solicitou o cancelamento"
    source = "api"
    cancellationType = "total"
    refundAmount = 200.00
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$completeSaleId/cancel" -Method Post -Headers $headers -Body $cancelBody
    Write-TestResult "Cancel Sale" $true $response 200
} catch {
    Write-TestResult "Cancel Sale" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 8: Filter Sales by Status ====================

Write-Host "TEST 8: Filter Sales by Status (Completed)" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales?status=completed&page=1&pageSize=10" -Method Get -Headers $headers
    Write-TestResult "Filter by Status" $true $response 200
} catch {
    Write-TestResult "Filter by Status" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 9: Filter Sales by Date Range ====================

Write-Host "TEST 9: Filter Sales by Date Range" -ForegroundColor Cyan

$dateFrom = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
$dateTo = (Get-Date).ToString("yyyy-MM-dd")

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales?dateFrom=$dateFrom&dateTo=$dateTo&page=1&pageSize=10" -Method Get -Headers $headers
    Write-TestResult "Filter by Date Range" $true $response 200
} catch {
    Write-TestResult "Filter by Date Range" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 10: Idempotency Test ====================

Write-Host "TEST 10: Idempotency Test (Same ClientSaleId)" -ForegroundColor Cyan

$clientSaleId = [guid]::NewGuid()

$idempotentBody = @{
    clientSaleId = $clientSaleId
    storeId = $storeId
    posId = $posId
    operatorId = $operatorId
    saleDateTime = (Get-Date).ToUniversalTime().ToString("o")
    items = @(
        @{
            productId = $productId1
            quantity = 1
            unitPrice = 50.00
            discountAmount = 0
        }
    )
} | ConvertTo-Json -Depth 10

try {
    # First request
    $response1 = Invoke-RestMethod -Uri "$BaseUrl/api/sales" -Method Post -Headers $headers -Body $idempotentBody
    $firstId = $response1.id
    
    # Second request with same clientSaleId
    $response2 = Invoke-RestMethod -Uri "$BaseUrl/api/sales" -Method Post -Headers $headers -Body $idempotentBody
    $secondId = $response2.id
    
    if ($firstId -eq $secondId) {
        Write-Host "✅ Idempotency Test - Same ID returned" -ForegroundColor Green
        Write-Host "   First ID:  $firstId" -ForegroundColor Gray
        Write-Host "   Second ID: $secondId" -ForegroundColor Gray
    } else {
        Write-Host "❌ Idempotency Test - Different IDs returned!" -ForegroundColor Red
        Write-Host "   First ID:  $firstId" -ForegroundColor Gray
        Write-Host "   Second ID: $secondId" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-TestResult "Idempotency Test" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== TEST 11: Update Sale Status ====================

Write-Host "TEST 11: Update Sale Status" -ForegroundColor Cyan

$updateBody = @{
    status = "processing"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sales/$saleId" -Method Patch -Headers $headers -Body $updateBody
    Write-TestResult "Update Sale Status" $true $response 200
} catch {
    Write-TestResult "Update Sale Status" $false $_.Exception.Message $_.Exception.Response.StatusCode.value__
}

# ==================== Summary ====================

Write-Host "==================== TEST SUMMARY ====================" -ForegroundColor Cyan
Write-Host "All tests completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Created Sale IDs:" -ForegroundColor Yellow
Write-Host "  - Pending Sale: $saleId" -ForegroundColor Gray
Write-Host "  - Complete Sale: $completeSaleId" -ForegroundColor Gray
Write-Host ""
Write-Host "To run again:" -ForegroundColor Yellow
Write-Host "  .\sales-tests.ps1 -BaseUrl '$BaseUrl' -Token 'your-jwt-token' -TenantId 'tenant_acme'" -ForegroundColor Gray
