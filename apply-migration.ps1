# PowerShell script to apply the Advanced Features migration
# This script uses the connection string from appsettings.json

$connectionString = "Host=localhost;Port=5432;Database=SuperMemo;Username=postgres;Password=M10m10m10;Pooling=true;MinPoolSize=5;MaxPoolSize=100;CommandTimeout=30;"

$sqlFile = "SuperMemo.Infrastructure\Migrations\20250207000000_AdvancedFeatures.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: Migration file not found at $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reading migration SQL file..." -ForegroundColor Yellow
$sql = Get-Content $sqlFile -Raw

# Split by semicolons and execute each statement
$statements = $sql -split ';' | Where-Object { $_.Trim() -ne '' -and $_.Trim() -notmatch '^--' }

Write-Host "Connecting to PostgreSQL database..." -ForegroundColor Yellow

try {
    # Try to use psql if available
    $env:PGPASSWORD = "M10m10m10"
    $result = & psql -h localhost -U postgres -d SuperMemo -f $sqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migration applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "Error applying migration:" -ForegroundColor Red
        Write-Host $result
        Write-Host ""
        Write-Host "Alternative: You can manually run the SQL file using:" -ForegroundColor Yellow
        Write-Host "psql -h localhost -U postgres -d SuperMemo -f $sqlFile" -ForegroundColor Cyan
    }
} catch {
    Write-Host "psql command not found. Please apply the migration manually:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Use pgAdmin or another PostgreSQL client" -ForegroundColor Cyan
    Write-Host "Option 2: Install PostgreSQL client tools and run:" -ForegroundColor Cyan
    Write-Host "  psql -h localhost -U postgres -d SuperMemo -f $sqlFile" -ForegroundColor White
    Write-Host ""
    Write-Host "Or use the connection string:" -ForegroundColor Yellow
    Write-Host "  $connectionString" -ForegroundColor White
}
