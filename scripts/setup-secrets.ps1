#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    NaijaShield AI — GitHub Actions secrets bootstrap.

.DESCRIPTION
    Automatically fetches Azure credentials (ACR login server, ACR credentials,
    service-principal JSON) using the Azure CLI, then prompts for the remaining
    secrets and sets them all via the GitHub CLI (gh).

    Prerequisites:
      1. Azure CLI  — az login  (already signed in)
      2. GitHub CLI — gh auth login  (already authenticated)

    Run once per repo. Safe to re-run — it overwrites existing secret values.

.PARAMETER ResourceGroup
    Azure resource group that contains the Container Registry.
    Defaults to prompting interactively.

.PARAMETER ResourceGroupStaging
    Resource group for the staging environment Container Apps.

.PARAMETER ResourceGroupProd
    Resource group for the production environment Container Apps.

.PARAMETER SubscriptionId
    Azure subscription ID. Defaults to the current 'az account' selection.

.EXAMPLE
    ./scripts/setup-secrets.ps1
    ./scripts/setup-secrets.ps1 -ResourceGroup naijashield-rg -ResourceGroupStaging naijashield-staging-rg -ResourceGroupProd naijashield-prod-rg
#>

param(
    [string]$ResourceGroup         = "",
    [string]$ResourceGroupStaging  = "",
    [string]$ResourceGroupProd     = "",
    [string]$SubscriptionId        = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helpers ───────────────────────────────────────────────────────────────────

function Write-Step([string]$msg)  { Write-Host "`n→ $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)    { Write-Host "  ✓ $msg"  -ForegroundColor Green }
function Write-Warn([string]$msg)  { Write-Host "  ⚠ $msg"  -ForegroundColor Yellow }
function Write-Fail([string]$msg)  { Write-Host "  ✗ $msg"  -ForegroundColor Red; exit 1 }

function Prompt-Value([string]$Label, [string]$Default = "") {
    $hint = if ($Default) { " [$Default]" } else { "" }
    $val  = Read-Host "  $Label$hint"
    return if ($val) { $val } else { $Default }
}

function Set-GhSecret([string]$Name, [string]$Value) {
    if (-not $Value) { Write-Warn "Skipping $Name — no value provided"; return }
    $Value | gh secret set $Name
    if ($LASTEXITCODE -ne 0) { Write-Fail "Failed to set secret: $Name" }
    Write-Ok "Set $Name"
}

# ── Preflight ─────────────────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

if (-not (Get-Command az   -ErrorAction SilentlyContinue)) { Write-Fail "Azure CLI (az) not found. Install from https://aka.ms/installazurecliwindows" }
if (-not (Get-Command gh   -ErrorAction SilentlyContinue)) { Write-Fail "GitHub CLI (gh) not found. Install from https://cli.github.com and run: gh auth login" }

# Confirm Azure login
$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) { Write-Fail "Not logged in to Azure. Run: az login" }

# Confirm GitHub login
gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Fail "Not logged in to GitHub CLI. Run: gh auth login" }

Write-Ok "Azure CLI and GitHub CLI are authenticated"

# ── Subscription ──────────────────────────────────────────────────────────────

if (-not $SubscriptionId) {
    $SubscriptionId = (az account show --query id -o tsv)
}
Write-Ok "Subscription: $SubscriptionId"

# ── Gather resource group names ───────────────────────────────────────────────

Write-Step "Resource group names"

if (-not $ResourceGroup) {
    Write-Host "`n  Available resource groups:" -ForegroundColor Gray
    az group list --query "[].name" -o tsv | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    $ResourceGroup = Prompt-Value "ACR resource group (contains your Container Registry)"
}

if (-not $ResourceGroupStaging) {
    $ResourceGroupStaging = Prompt-Value "Staging resource group" $ResourceGroup
}

if (-not $ResourceGroupProd) {
    $ResourceGroupProd = Prompt-Value "Production resource group" $ResourceGroup
}

# ── Azure Container Registry ──────────────────────────────────────────────────

Write-Step "Fetching ACR details from Azure"

$acrName = az acr list --resource-group $ResourceGroup --query "[0].name" -o tsv 2>&1
if ($LASTEXITCODE -ne 0 -or -not $acrName) {
    Write-Warn "Could not find an ACR in resource group '$ResourceGroup'."
    $acrName = Prompt-Value "ACR name (e.g. naijashieldacr)"
}

$acrLoginServer = az acr show --name $acrName --query loginServer -o tsv
Write-Ok "ACR: $acrLoginServer"

# Enable admin user if not already (needed for username/password creds)
az acr update --name $acrName --admin-enabled true | Out-Null

$acrCreds      = az acr credential show --name $acrName | ConvertFrom-Json
$acrUsername   = $acrCreds.username
$acrPassword   = $acrCreds.passwords[0].value

Write-Ok "ACR credentials retrieved"

# ── Service Principal (AZURE_CREDENTIALS) ─────────────────────────────────────

Write-Step "Creating / retrieving Azure service principal for CI/CD"

$spName = "naijashield-github-actions"

# Check if SP already exists
$existingSp = az ad sp list --display-name $spName --query "[0].appId" -o tsv 2>&1

if ($existingSp -and $existingSp -ne "") {
    Write-Warn "Service principal '$spName' already exists (appId: $existingSp)."
    $recreate = Read-Host "  Recreate credentials? (y/N)"
    if ($recreate -eq 'y' -or $recreate -eq 'Y') {
        $azCreds = az ad sp create-for-rbac `
            --name $spName `
            --role Contributor `
            --scopes "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupStaging" `
                     "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupProd" `
            --sdk-auth `
            -o json
    } else {
        Write-Warn "Skipping AZURE_CREDENTIALS — keeping existing SP credentials."
        Write-Warn "If the secret isn't set yet, paste the existing SP JSON when prompted below."
        $azCreds = Prompt-Value "  Paste existing AZURE_CREDENTIALS JSON (or leave blank to skip)"
    }
} else {
    $azCreds = az ad sp create-for-rbac `
        --name $spName `
        --role Contributor `
        --scopes "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupStaging" `
                 "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupProd" `
        --sdk-auth `
        -o json
}

# Also grant AcrPush role so the SP can push images
if ($existingSp) {
    $spAppId = $existingSp
} else {
    $spAppId = ($azCreds | ConvertFrom-Json).clientId
}
az role assignment create `
    --assignee $spAppId `
    --role AcrPush `
    --scope (az acr show --name $acrName --query id -o tsv) 2>&1 | Out-Null

Write-Ok "Service principal ready, AcrPush role assigned"

# ── SQL Connection Strings ─────────────────────────────────────────────────────

Write-Step "SQL Server connection strings"
Write-Host "  Format: Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;..." -ForegroundColor Gray
Write-Host "  Tip: copy from Azure Portal → SQL Database → Connection strings → ADO.NET" -ForegroundColor Gray

$sqlStaging = Prompt-Value "SQL_CONN_STAGING"
$sqlProd    = Prompt-Value "SQL_CONN_PROD (leave blank to reuse staging value)" $sqlStaging

# ── Frontend URLs ──────────────────────────────────────────────────────────────

Write-Step "Frontend URLs (for CORS allow-list)"
Write-Host "  These are the origins the API will accept requests from." -ForegroundColor Gray
Write-Host "  Production default is already https://app.naijashield.ng" -ForegroundColor Gray

$frontendStaging = Prompt-Value "FRONTEND_URL_STAGING (e.g. https://staging.app.naijashield.ng)" "https://staging.app.naijashield.ng"
$frontendProd    = Prompt-Value "FRONTEND_URL_PROD    (e.g. https://app.naijashield.ng)" "https://app.naijashield.ng"

# ── Set all secrets ───────────────────────────────────────────────────────────

Write-Step "Setting GitHub Actions secrets"

Set-GhSecret "AZURE_CREDENTIALS"   $azCreds
Set-GhSecret "ACR_REGISTRY"        $acrLoginServer
Set-GhSecret "ACR_USERNAME"        $acrUsername
Set-GhSecret "ACR_PASSWORD"        $acrPassword
Set-GhSecret "AZURE_RG_STAGING"    $ResourceGroupStaging
Set-GhSecret "AZURE_RG_PROD"       $ResourceGroupProd
Set-GhSecret "SQL_CONN_STAGING"    $sqlStaging
Set-GhSecret "SQL_CONN_PROD"       $sqlProd
Set-GhSecret "FRONTEND_URL_STAGING" $frontendStaging
Set-GhSecret "FRONTEND_URL_PROD"    $frontendProd

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GitHub Actions secrets configured!" -ForegroundColor Green
Write-Host ""
Write-Host "  Secrets set:" -ForegroundColor White
Write-Host "    AZURE_CREDENTIALS      — service principal JSON" -ForegroundColor Gray
Write-Host "    ACR_REGISTRY           — $acrLoginServer" -ForegroundColor Gray
Write-Host "    ACR_USERNAME           — $acrUsername" -ForegroundColor Gray
Write-Host "    ACR_PASSWORD           — (hidden)" -ForegroundColor Gray
Write-Host "    AZURE_RG_STAGING       — $ResourceGroupStaging" -ForegroundColor Gray
Write-Host "    AZURE_RG_PROD          — $ResourceGroupProd" -ForegroundColor Gray
Write-Host "    SQL_CONN_STAGING       — (hidden)" -ForegroundColor Gray
Write-Host "    SQL_CONN_PROD          — (hidden)" -ForegroundColor Gray
Write-Host "    FRONTEND_URL_STAGING   — $frontendStaging" -ForegroundColor Gray
Write-Host "    FRONTEND_URL_PROD      — $frontendProd" -ForegroundColor Gray
Write-Host ""
Write-Host "  Next: push to main to trigger the deploy pipeline." -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
