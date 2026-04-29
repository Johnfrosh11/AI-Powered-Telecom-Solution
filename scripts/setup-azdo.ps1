#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    NaijaShield AI — Azure DevOps one-time bootstrap.

.DESCRIPTION
    Creates and configures everything needed to run the Azure Pipelines CI/CD:
      1. Azure DevOps project  (NaijaShield)
      2. Azure RM service connection  (naijashield-azure-rm)
      3. Docker / ACR service connection  (naijashield-acr)
      4. Variable group  (naijashield-vars) with all pipeline secrets
      5. Environments  (staging, production)
      6. Pipeline linked to azure-pipelines.yml in this repo

    After this script succeeds:
      • Go to Pipelines > Environments > production > Approvals
        and add yourself as a required approver for the production gate.
      • Push to main — the pipeline will trigger automatically.

    Prerequisites:
      az login           (Azure CLI signed in)
      az devops org URL  (dev.azure.com/team-6-B)

.PARAMETER OrgUrl
    Azure DevOps organisation URL. Defaults to https://dev.azure.com/team-6-B

.PARAMETER ProjectName
    Name for the Azure DevOps project. Defaults to NaijaShield

.PARAMETER ResourceGroup
    Azure resource group that contains the ACR.

.PARAMETER ResourceGroupStaging
    Resource group for staging Container Apps.

.PARAMETER ResourceGroupProd
    Resource group for production Container Apps.

.EXAMPLE
    ./scripts/setup-azdo.ps1
    ./scripts/setup-azdo.ps1 -OrgUrl https://dev.azure.com/team-6-B -ProjectName NaijaShield
#>

param(
    [string]$OrgUrl               = "https://dev.azure.com/team-6-B",
    [string]$ProjectName          = "NaijaShield",
    [string]$ResourceGroup        = "",
    [string]$ResourceGroupStaging = "",
    [string]$ResourceGroupProd    = ""
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

# ── Preflight ─────────────────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) { Write-Fail "Not logged in. Run: az login" }

# Ensure azure-devops extension is present
az extension add --name azure-devops --only-show-errors 2>&1 | Out-Null
Write-Ok "Azure CLI + DevOps extension ready"

# Set default org for all az devops commands
$env:AZURE_DEVOPS_EXT_PAT = ""   # use az credential, not PAT
az devops configure --defaults organization=$OrgUrl 2>&1 | Out-Null

$SubscriptionId   = (az account show --query id   -o tsv)
$SubscriptionName = (az account show --query name -o tsv)
Write-Ok "Subscription: $SubscriptionName ($SubscriptionId)"

# ── Resource group names ──────────────────────────────────────────────────────

Write-Step "Resource group names"

if (-not $ResourceGroup) {
    Write-Host "`n  Available resource groups:" -ForegroundColor Gray
    az group list --query "[].name" -o tsv | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    $ResourceGroup = Prompt-Value "ACR resource group"
}
if (-not $ResourceGroupStaging) { $ResourceGroupStaging = Prompt-Value "Staging resource group" $ResourceGroup }
if (-not $ResourceGroupProd)    { $ResourceGroupProd    = Prompt-Value "Production resource group" $ResourceGroup }

# ── ACR details ───────────────────────────────────────────────────────────────

Write-Step "Fetching ACR details"

$acrName = az acr list --resource-group $ResourceGroup --query "[0].name" -o tsv 2>&1
if ($LASTEXITCODE -ne 0 -or -not $acrName) {
    $acrName = Prompt-Value "ACR name (e.g. naijashieldacr)"
}

$acrLoginServer = az acr show --name $acrName --query loginServer -o tsv
az acr update --name $acrName --admin-enabled true | Out-Null
$acrCreds       = az acr credential show --name $acrName | ConvertFrom-Json
$acrUsername    = $acrCreds.username
$acrPassword    = $acrCreds.passwords[0].value

Write-Ok "ACR: $acrLoginServer"

# ── SQL connection strings & frontend URLs ────────────────────────────────────

Write-Step "Pipeline secret values"
Write-Host "  (These are stored in Azure DevOps Library, not committed to the repo)" -ForegroundColor Gray

$sqlStaging      = Prompt-Value "SQL_CONN_STAGING"
$sqlProd         = Prompt-Value "SQL_CONN_PROD (blank = same as staging)" $sqlStaging
$frontendStaging = Prompt-Value "FRONTEND_URL_STAGING" "https://staging.app.naijashield.ng"
$frontendProd    = Prompt-Value "FRONTEND_URL_PROD"    "https://app.naijashield.ng"

# ── 1. Create Azure DevOps project ───────────────────────────────────────────

Write-Step "Creating Azure DevOps project: $ProjectName"

$existingProject = az devops project show --project $ProjectName --org $OrgUrl 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Warn "Project '$ProjectName' already exists — skipping creation"
} else {
    az devops project create `
        --name $ProjectName `
        --org $OrgUrl `
        --visibility private `
        --source-control git | Out-Null
    Write-Ok "Project '$ProjectName' created"
}

az devops configure --defaults project=$ProjectName 2>&1 | Out-Null

# ── 2. Azure RM service connection ───────────────────────────────────────────

Write-Step "Creating Azure RM service connection: naijashield-azure-rm"

# Create service principal for the connection
$sp = az ad sp create-for-rbac `
    --name "naijashield-azdo-pipeline" `
    --role Contributor `
    --scopes "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupStaging" `
             "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupProd" `
    --output json | ConvertFrom-Json

# Grant AcrPush
$acrId = az acr show --name $acrName --query id -o tsv
az role assignment create --assignee $sp.appId --role AcrPush --scope $acrId | Out-Null

$tenantId = (az account show --query tenantId -o tsv)

# Build the service endpoint JSON
$epJson = @{
    data = @{
        subscriptionId   = $SubscriptionId
        subscriptionName = $SubscriptionName
        environment      = "AzureCloud"
        scopeLevel       = "Subscription"
        creationMode     = "Manual"
    }
    name  = "naijashield-azure-rm"
    type  = "AzureRM"
    url   = "https://management.azure.com/"
    authorization = @{
        parameters = @{
            tenantid            = $tenantId
            serviceprincipalid  = $sp.appId
            authenticationType  = "spnKey"
            serviceprincipalkey = $sp.password
        }
        scheme = "ServicePrincipal"
    }
    isShared    = $false
    isReady     = $true
    serviceEndpointProjectReferences = @(@{
        projectReference = @{
            id   = (az devops project show --project $ProjectName --query id -o tsv)
            name = $ProjectName
        }
        name = "naijashield-azure-rm"
    })
} | ConvertTo-Json -Depth 10

$tmpEp = Join-Path $env:TEMP "naijashield_ep.json"
Set-Content $tmpEp $epJson

az devops service-endpoint create --service-endpoint-configuration $tmpEp | Out-Null
Remove-Item $tmpEp -Force

Write-Ok "Service connection 'naijashield-azure-rm' created"

# ── 3. Docker / ACR service connection ───────────────────────────────────────

Write-Step "Creating Docker registry service connection: naijashield-acr"

$env:AZURE_DEVOPS_EXT_AZURE_RM_SERVICE_PRINCIPAL_KEY = $acrPassword
az devops service-endpoint azurerm create 2>&1 | Out-Null   # dummy — use docker endpoint

# Use the REST API shortcut via az devops service-endpoint create
$dockerEpJson = @{
    name = "naijashield-acr"
    type = "dockerregistry"
    url  = "https://$acrLoginServer"
    authorization = @{
        parameters = @{
            username = $acrUsername
            password = $acrPassword
            email    = "ci@naijashield.ai"
            registry = "https://$acrLoginServer"
        }
        scheme = "UsernamePassword"
    }
    data = @{ registrytype = "Others" }
    isShared = $false
    isReady  = $true
    serviceEndpointProjectReferences = @(@{
        projectReference = @{
            id   = (az devops project show --project $ProjectName --query id -o tsv)
            name = $ProjectName
        }
        name = "naijashield-acr"
    })
} | ConvertTo-Json -Depth 10

$tmpDockerEp = Join-Path $env:TEMP "naijashield_docker_ep.json"
Set-Content $tmpDockerEp $dockerEpJson
az devops service-endpoint create --service-endpoint-configuration $tmpDockerEp | Out-Null
Remove-Item $tmpDockerEp -Force

Write-Ok "Service connection 'naijashield-acr' created"

# ── 4. Variable group ─────────────────────────────────────────────────────────

Write-Step "Creating variable group: naijashield-vars"

$groupId = az pipelines variable-group create `
    --name naijashield-vars `
    --variables `
        ACR_REGISTRY=$acrLoginServer `
        ACR_USERNAME=$acrUsername `
        AZURE_RG_STAGING=$ResourceGroupStaging `
        AZURE_RG_PROD=$ResourceGroupProd `
        FRONTEND_URL_STAGING=$frontendStaging `
        FRONTEND_URL_PROD=$frontendProd `
    --query id -o tsv

# Add secrets (marked as secret so values are masked in logs)
az pipelines variable-group variable create --group-id $groupId --name ACR_PASSWORD         --value $acrPassword   --secret true | Out-Null
az pipelines variable-group variable create --group-id $groupId --name SQL_CONN_STAGING     --value $sqlStaging    --secret true | Out-Null
az pipelines variable-group variable create --group-id $groupId --name SQL_CONN_PROD        --value $sqlProd       --secret true | Out-Null

Write-Ok "Variable group 'naijashield-vars' created (id: $groupId)"

# ── 5. Environments ───────────────────────────────────────────────────────────

Write-Step "Creating pipeline environments"

# Environments are created automatically when first referenced by a deployment job,
# but we create them now so we can add approval gates immediately.
foreach ($env in @("staging", "production")) {
    az devops invoke `
        --area distributedtask `
        --resource environments `
        --route-parameters project=$ProjectName `
        --http-method POST `
        --in-file (([System.IO.Path]::GetTempFileName()) | % { Set-Content $_ "{`"name`":`"$env`",`"description`":`"`"}"; $_ }) `
        2>&1 | Out-Null
    Write-Ok "Environment '$env' created"
}

# ── 6. Create pipeline ────────────────────────────────────────────────────────

Write-Step "Creating pipeline from azure-pipelines.yml"

$repoUrl = (git -C (Split-Path $PSScriptRoot -Parent) remote get-url origin)

az pipelines create `
    --name "NaijaShield CI/CD" `
    --repository $repoUrl `
    --repository-type github `
    --branch main `
    --yml-path azure-pipelines.yml `
    --skip-first-run | Out-Null

Write-Ok "Pipeline 'NaijaShield CI/CD' created"

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Azure DevOps bootstrap complete!" -ForegroundColor Green
Write-Host ""
Write-Host "  One manual step remaining:" -ForegroundColor Yellow
Write-Host "    $OrgUrl/$ProjectName/_environments" -ForegroundColor White
Write-Host "    → Open 'production' → Approvals and checks → + Add" -ForegroundColor White
Write-Host "    → Choose 'Approvals', add yourself as required approver" -ForegroundColor White
Write-Host ""
Write-Host "  Then push to main — the pipeline will trigger." -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
