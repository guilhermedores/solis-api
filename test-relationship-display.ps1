# Test script to verify relationship display values are working
Write-Host "=== Testing Relationship Display Values ===" -ForegroundColor Cyan
Write-Host ""

# Direct SQL query to see what we expect
Write-Host "1. Direct SQL query (what we expect to see):" -ForegroundColor Yellow
docker exec solis-postgres psql -U solis_user -d solis_pdv -c "SELECT c.id, c.legal_name, c.tax_regime_id, t.description as tax_regime_id_display FROM tenant_demo.companies c LEFT JOIN tenant_demo.tax_regimes t ON c.tax_regime_id = t.id LIMIT 1;"

Write-Host ""
Write-Host "2. Checking entity metadata for company:" -ForegroundColor Yellow
$metadataUrl = "http://localhost:5287/api/dynamic/company/_metadata"
Write-Host "   GET $metadataUrl" -ForegroundColor Gray

try {
    $metadata = Invoke-RestMethod -Uri $metadataUrl -Method Get -Headers @{
        'X-Tenant-Subdomain' = 'demo'
    }
    
    Write-Host "   Fields with ShowInList=true:" -ForegroundColor Green
    $metadata.entity.fields | Where-Object { $_.showInList -eq $true } | ForEach-Object {
        $rel = if ($_.relationship) { " (→ $($_.relationship.relatedEntityName))" } else { "" }
        Write-Host "     - $($_.name)$rel" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "   Relationships configured:" -ForegroundColor Green
    $metadata.entity.relationships | ForEach-Object {
        Write-Host "     - Field: $($_.fieldId) → $($_.relatedEntityName) (display: $($_.displayField))" -ForegroundColor White
    }
} catch {
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Testing list endpoint (should include tax_regime_id_display):" -ForegroundColor Yellow
$listUrl = "http://localhost:5287/api/dynamic/company?page=1&pageSize=10"
Write-Host "   GET $listUrl" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri $listUrl -Method Get -Headers @{
        'X-Tenant-Subdomain' = 'demo'
    }
    
    Write-Host "   Success: $($response.success)" -ForegroundColor Green
    Write-Host "   Total records: $($response.totalCount)" -ForegroundColor Green
    Write-Host ""
    
    if ($response.data -and $response.data.Count -gt 0) {
        Write-Host "   First company data:" -ForegroundColor Cyan
        $firstCompany = $response.data[0].data
        
        # Show all properties
        $firstCompany.PSObject.Properties | ForEach-Object {
            $color = if ($_.Name -like "*_display") { "Green" } else { "White" }
            $value = if ($_.Value -is [string] -and $_.Value.Length -gt 50) { 
                $_.Value.Substring(0, 47) + "..."
            } else {
                $_.Value
            }
            Write-Host "     $($_.Name): $value" -ForegroundColor $color
        }
        
        # Check if display field exists
        Write-Host ""
        if ($firstCompany.PSObject.Properties.Name -contains "tax_regime_id_display") {
            Write-Host "   ✅ SUCCESS: tax_regime_id_display field is present!" -ForegroundColor Green
            Write-Host "   Value: $($firstCompany.tax_regime_id_display)" -ForegroundColor Green
        } else {
            Write-Host "   ❌ FAIL: tax_regime_id_display field is missing" -ForegroundColor Red
        }
        
        if ($firstCompany.PSObject.Properties.Name -contains "special_tax_regime_id_display") {
            Write-Host "   ✅ SUCCESS: special_tax_regime_id_display field is present!" -ForegroundColor Green
            Write-Host "   Value: $($firstCompany.special_tax_regime_id_display)" -ForegroundColor Green
        }
    } else {
        Write-Host "   No data returned" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Error: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
