#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    NaijaShield AI — Demo-ready seed script.

.DESCRIPTION
    1. Starts Docker Compose (SQL Server + Redis)
    2. Waits for SQL Server to be healthy
    3. Runs EF Core migrations
    4. Starts the API briefly to trigger DataSeeder (dev mode)
    5. Generates 50 fake ScamCall records via direct SQL for demo dashboards

.PARAMETER SkipDocker
    Skip starting Docker Compose (use when containers are already running)

.PARAMETER SkipMigrations
    Skip running EF Core migrations

.PARAMETER FakeCallCount
    Number of fake ScamCall records to generate (default: 50)

.EXAMPLE
    ./scripts/seed.ps1
    ./scripts/seed.ps1 -SkipDocker -FakeCallCount 100
#>

param(
    [switch]$SkipDocker,
    [switch]$SkipMigrations,
    [int]$FakeCallCount = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root          = Split-Path $PSScriptRoot -Parent
$BackendDir    = Join-Path $Root "backend"
$ApiProject    = Join-Path $BackendDir "src/NaijaShield.Api"
$InfraProject  = Join-Path $BackendDir "src/NaijaShield.Infrastructure"

$SqlConnString = "Server=localhost,1433;Database=NaijaShieldDev;User Id=sa;Password=NaijaShield@Dev1;TrustServerCertificate=True"
$ApiBaseUrl    = "http://localhost:5000"

# ── Colours ───────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) { Write-Host "`n→ $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "  ✓ $msg"  -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "  ⚠ $msg"  -ForegroundColor Yellow }
function Write-Fail([string]$msg) { Write-Host "  ✗ $msg"  -ForegroundColor Red; exit 1 }

# ── Step 1: Docker Compose ────────────────────────────────────────────────────

if (-not $SkipDocker) {
    Write-Step "Starting Docker Compose (SQL Server + Redis)"

    Push-Location $Root
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { Write-Fail "docker compose up failed" }
    Pop-Location

    Write-Ok "Docker Compose started"
}

# ── Step 2: Wait for SQL Server ───────────────────────────────────────────────

Write-Step "Waiting for SQL Server to be healthy..."

$maxAttempts = 30
$attempt     = 0
$ready       = $false

while (-not $ready -and $attempt -lt $maxAttempts) {
    $attempt++
    try {
        $result = docker exec naijashield-sql /opt/mssql-tools/bin/sqlcmd `
            -S localhost -U sa -P 'NaijaShield@Dev1' `
            -Q "SELECT 1" -l 5 2>&1

        if ($result -match "1 rows affected" -or $result -match "---") {
            $ready = $true
        }
    } catch { }

    if (-not $ready) {
        Write-Host "  Attempt $attempt/$maxAttempts — waiting 3s..." -ForegroundColor Gray
        Start-Sleep -Seconds 3
    }
}

if (-not $ready) { Write-Fail "SQL Server did not become ready after $($maxAttempts * 3) seconds" }
Write-Ok "SQL Server is healthy"

# ── Step 3: EF Core Migrations ────────────────────────────────────────────────

if (-not $SkipMigrations) {
    Write-Step "Running EF Core migrations..."

    Push-Location $BackendDir

    $env:ConnectionStrings__DefaultConnection = $SqlConnString

    dotnet ef database update `
        --project $InfraProject `
        --startup-project $ApiProject `
        --no-build 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

    if ($LASTEXITCODE -ne 0) { Write-Fail "EF Core migrations failed" }

    Pop-Location
    Write-Ok "Migrations applied"
}

# ── Step 4: Trigger DataSeeder via API start ──────────────────────────────────

Write-Step "Starting API to trigger DataSeeder (dev mode)..."

$env:ASPNETCORE_ENVIRONMENT   = "Development"
$env:ASPNETCORE_URLS           = $ApiBaseUrl
$env:ConnectionStrings__DefaultConnection = $SqlConnString

Push-Location $BackendDir

$apiProcess = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList "run --project src/NaijaShield.Api --no-build" `
    -PassThru `
    -NoNewWindow `
    -RedirectStandardOutput (Join-Path $BackendDir "api-seed.log")

# Wait for API to be up
$apiReady   = $false
$maxWait    = 60
$waited     = 0

while (-not $apiReady -and $waited -lt $maxWait) {
    try {
        $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -TimeoutSec 3 -ErrorAction Stop
        if ($response.StatusCode -eq 200) { $apiReady = $true }
    } catch { }

    if (-not $apiReady) {
        Start-Sleep -Seconds 2
        $waited += 2
    }
}

# Stop API (seeder ran on startup)
Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
Pop-Location

if ($apiReady) {
    Write-Ok "API started, DataSeeder executed, API stopped"
} else {
    Write-Warn "API health check timed out — DataSeeder may not have run. Check api-seed.log"
}

# ── Step 5: Generate fake ScamCall demo data ──────────────────────────────────

Write-Step "Generating $FakeCallCount fake ScamCall records for demo dashboards..."

# We need the MTN tenant ID from the seeder (stable GUID)
$mtnTenantId = "10000000-0000-0000-0000-000000000001"

$callers = @(
    "2348012345678", "2348023456789", "2348034567890", "2349012345678",
    "2349023456789", "2347012345678", "2347023456789", "2348112345678"
)
$statuses = @("Confirmed", "FalsePositive", "Pending", "Blocked")
$languages = @("en", "pidgin", "yo", "ha", "ig")
$reasonings = @(
    "Caller requested OTP using social engineering tactics consistent with bank impersonation pattern.",
    "Caller offered lottery prize and requested payment of processing fee — classic advance fee fraud.",
    "SIM swap attempt detected: caller requested NIN and account details under pretence of network upgrade.",
    "Caller impersonated EFCC officer, demanded payment to avoid arrest. High confidence scam.",
    "Investment scheme pitch with guaranteed 50% return in 7 days. Matches Ponzi pattern.",
    "Romance scam pattern: caller building rapport, likely to request funds in future calls.",
    "Job offer scam: Dubai job offer with upfront registration fee requirement.",
    "Fake loan offer with upfront insurance fee payment demanded.",
    "Recharge card scam: caller requested airtime codes under pretence of technical support.",
    "Tech support impersonation: caller claimed victim phone is hacked and requested remote access."
)

$insertSql = @()
$now = [DateTime]::UtcNow

for ($i = 0; $i -lt $FakeCallCount; $i++) {
    $id           = [Guid]::NewGuid().ToString()
    $caller       = $callers | Get-Random
    $receiverSuffix = Get-Random -Minimum 10000000 -Maximum 99999999
    $receiver     = "234803$receiverSuffix"
    $daysAgo      = Get-Random -Minimum 0 -Maximum 30
    $hoursAgo     = Get-Random -Minimum 0 -Maximum 23
    $startedAt    = $now.AddDays(-$daysAgo).AddHours(-$hoursAgo).ToString("yyyy-MM-dd HH:mm:ss")
    $durationSec  = Get-Random -Minimum 30 -Maximum 600
    $language     = $languages | Get-Random
    $confidence   = [Math]::Round((Get-Random -Minimum 70 -Maximum 99) / 100.0, 4)
    $status       = $statuses | Get-Random
    $warningSent  = if ($status -eq "Confirmed") { 1 } else { 0 }
    $victims      = if ($warningSent) { Get-Random -Minimum 1 -Maximum 8 } else { 0 }
    $moneySaved   = if ($warningSent) { [Math]::Round((Get-Random -Minimum 5000 -Maximum 500000), 2) } else { "NULL" }
    $reasoning    = $reasonings | Get-Random
    $reasoning    = $reasoning.Replace("'", "''")

    $moneySavedSql = if ($moneySaved -eq "NULL") { "NULL" } else { $moneySaved }

    $insertSql += @"
INSERT INTO ScamCalls (Id, TenantId, CallerMsisdn, ReceiverMsisdn, StartedAt, Duration, DetectedLanguage,
    AiConfidenceScore, Status, WarningSmsSent, VictimsWarned, EstimatedMoneySaved, AiReasoning,
    CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    '$id', '$mtnTenantId', '$caller', '$receiver', '$startedAt',
    '00:$(([TimeSpan]::FromSeconds($durationSec)).ToString("mm\:ss"))',
    '$language', $confidence, '$status', $warningSent, $victims, $moneySavedSql,
    '$reasoning', '$startedAt', '$startedAt', 0
);
"@
}

$sqlBatch = $insertSql -join "`n"
$tmpSqlFile = Join-Path $env:TEMP "naijashield_seed_calls.sql"
Set-Content -Path $tmpSqlFile -Value "USE NaijaShieldDev;`n$sqlBatch"

docker exec -i naijashield-sql /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P 'NaijaShield@Dev1' `
    -i /dev/stdin < $tmpSqlFile 2>&1 | ForEach-Object {
    if ($_ -match "error|Error") { Write-Warn $_ } else { }
}

Remove-Item $tmpSqlFile -Force -ErrorAction SilentlyContinue

if ($LASTEXITCODE -eq 0) {
    Write-Ok "Inserted $FakeCallCount fake ScamCall records"
} else {
    Write-Warn "Some inserts may have failed — check output above"
}

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  NaijaShield AI — Demo Environment Ready!" -ForegroundColor Green
Write-Host ""
Write-Host "  Admin login:" -ForegroundColor White
Write-Host "    Email:    admin@mtn.naijashield.ai" -ForegroundColor Yellow
Write-Host "    Password: NaijaShield@2024!" -ForegroundColor Yellow
Write-Host ""
Write-Host "  API:     $ApiBaseUrl" -ForegroundColor White
Write-Host "  Swagger: $ApiBaseUrl (root)" -ForegroundColor White
Write-Host ""
Write-Host "  Fake scam calls generated: $FakeCallCount" -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
