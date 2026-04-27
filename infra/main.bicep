// NaijaShield AI — Azure Infrastructure
// Provisions all Azure resources needed for the NaijaShield AI platform.
//
// Resources:
//   Log Analytics Workspace + Application Insights
//   Azure SQL (Business Critical prod / General Purpose dev)
//   Container Registry (ACR)
//   Container Apps Environment + 2 Container Apps (api + jobs)
//   Azure Key Vault
//   Service Bus (Standard tier)
//   Azure OpenAI (gpt-4o + gpt-4o-mini deployments)
//   Azure Cache for Redis
//   Storage Account (blob containers)
//   Azure Front Door + WAF Policy

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Deployment environment: dev | staging | prod')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Azure AD tenant ID')
param tenantId string = subscription().tenantId

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('Object ID of the AAD group / user that gets Key Vault admin rights')
param keyVaultAdminObjectId string = ''

@description('API container image (ACR path + tag)')
param apiImageTag string = 'latest'

@description('Background jobs container image (ACR path + tag)')
param jobsImageTag string = 'latest'

// ── Variables ─────────────────────────────────────────────────────────────────

var prefix = 'naijashield'
var envSuffix = environment == 'prod' ? 'prd' : environment == 'staging' ? 'stg' : 'dev'
var isProd = environment == 'prod'
var sqlSku = isProd ? 'BusinessCritical' : 'GeneralPurpose'
var sqlCapacity = isProd ? 8 : 2
var redisSku = isProd ? 'Standard' : 'Basic'
var redisFamilyCode = isProd ? 'C' : 'C'
var redisCapacity = isProd ? 1 : 0
var acrSku = isProd ? 'Premium' : 'Basic'

var tags = {
  project: 'NaijaShield-AI'
  environment: environment
  managedBy: 'Bicep'
}

// ── Log Analytics + Application Insights ──────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs-${envSuffix}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: isProd ? 90 : 30
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-ai-${envSuffix}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ── Azure Container Registry ──────────────────────────────────────────────────

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${prefix}acr${envSuffix}'
  location: location
  tags: tags
  sku: { name: acrSku }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
  }
}

// ── Azure SQL ─────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: '${prefix}-sql-${envSuffix}'
  location: location
  tags: tags
  properties: {
    administratorLogin: 'naijashieldadmin'
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: isProd ? 'Disabled' : 'Enabled'
  }
}

resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (!isProd) {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: '${prefix}-db-${envSuffix}'
  location: location
  tags: tags
  sku: {
    name: sqlSku
    tier: sqlSku
    capacity: sqlCapacity
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: isProd ? 107374182400 : 34359738368  // 100GB prod / 32GB dev
    zoneRedundant: isProd
    readScale: isProd ? 'Enabled' : 'Disabled'
    requestedBackupStorageRedundancy: isProd ? 'Geo' : 'Local'
  }
}

// ── Azure Cache for Redis ─────────────────────────────────────────────────────

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${prefix}-cache-${envSuffix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: redisSku
      family: redisFamilyCode
      capacity: redisCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// ── Azure Service Bus ─────────────────────────────────────────────────────────

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${prefix}-sb-${envSuffix}'
  location: location
  tags: tags
  sku: { name: 'Standard', tier: 'Standard' }
  properties: {
    minimumTlsVersion: '1.2'
    disableLocalAuth: false
  }
}

resource sbQueueOutbox 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'outbox-messages'
  properties: {
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

resource sbQueueFraudAlerts 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'fraud-alerts'
  properties: {
    lockDuration: 'PT1M'
    maxSizeInMegabytes: 1024
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 5
  }
}

// ── Storage Account ───────────────────────────────────────────────────────────

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${prefix}stor${envSuffix}'
  location: location
  tags: tags
  sku: { name: isProd ? 'Standard_GRS' : 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource containerReports 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'regulatory-reports'
  properties: { publicAccess: 'None' }
}

resource containerExports 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'data-exports'
  properties: { publicAccess: 'None' }
}

resource containerAudioRecordings 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'audio-recordings'
  properties: { publicAccess: 'None' }
}

// ── Azure Key Vault ───────────────────────────────────────────────────────────

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${prefix}-kv-${envSuffix}'
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: isProd ? 90 : 7
    enablePurgeProtection: isProd ? true : false
    networkAcls: {
      defaultAction: isProd ? 'Deny' : 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Grant Key Vault admin if objectId provided
resource kvAdminRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (keyVaultAdminObjectId != '') {
  name: guid(keyVault.id, keyVaultAdminObjectId, 'Key Vault Administrator')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00482a5a-887f-4fb3-b363-3b7fe8e74483')
    principalId: keyVaultAdminObjectId
    principalType: 'User'
  }
}

// ── Azure OpenAI ──────────────────────────────────────────────────────────────

resource openAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: '${prefix}-aoai-${envSuffix}'
  location: 'eastus'           // Azure OpenAI availability region
  tags: tags
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: '${prefix}-aoai-${envSuffix}'
    publicNetworkAccess: 'Enabled'
    networkAcls: { defaultAction: 'Allow' }
  }
}

resource openAIGpt4o 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: 'gpt-4o'
  sku: { name: 'Standard', capacity: isProd ? 80 : 10 }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
  }
}

resource openAIGpt4oMini 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: 'gpt-4o-mini'
  sku: { name: 'Standard', capacity: isProd ? 200 : 30 }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
  }
  dependsOn: [openAIGpt4o]
}

// ── Container Apps Environment ────────────────────────────────────────────────

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: '${prefix}-cae-${envSuffix}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
  }
}

// ── Container App — API ───────────────────────────────────────────────────────

resource apiContainerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: '${prefix}-api-${envSuffix}'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        corsPolicy: {
          allowedOrigins: isProd
            ? ['https://app.naijashield.ai']
            : ['https://app-staging.naijashield.ai', 'http://localhost:3000']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS']
          allowedHeaders: ['*']
          allowCredentials: true
        }
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/SqlConnectionString'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.properties.loginServer}/naijashield-api:${apiImageTag}'
          name: 'api'
          resources: {
            cpu: json(isProd ? '2.0' : '0.5')
            memory: isProd ? '4Gi' : '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: isProd ? 'Production' : 'Staging' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
            { name: 'ConnectionStrings__DefaultConnection', secretRef: 'sql-connection-string' }
            { name: 'Redis__ConnectionString', value: '${redis.properties.hostName}:6380,ssl=True,abortConnect=False' }
            { name: 'AzureOpenAI__Endpoint', value: openAI.properties.endpoint }
          ]
        }
      ]
      scale: {
        minReplicas: isProd ? 2 : 1
        maxReplicas: isProd ? 20 : 5
        rules: [
          {
            name: 'http-scaling'
            http: { metadata: { concurrentRequests: '50' } }
          }
        ]
      }
    }
  }
}

// ── Container App — Background Jobs ──────────────────────────────────────────

resource jobsContainerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: '${prefix}-jobs-${envSuffix}'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: null          // internal only — no public ingress for jobs
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/SqlConnectionString'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${acr.properties.loginServer}/naijashield-jobs:${jobsImageTag}'
          name: 'jobs'
          resources: {
            cpu: json(isProd ? '1.0' : '0.25')
            memory: isProd ? '2Gi' : '0.5Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: isProd ? 'Production' : 'Staging' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
            { name: 'ConnectionStrings__DefaultConnection', secretRef: 'sql-connection-string' }
            { name: 'Redis__ConnectionString', value: '${redis.properties.hostName}:6380,ssl=True,abortConnect=False' }
            { name: 'AzureOpenAI__Endpoint', value: openAI.properties.endpoint }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: isProd ? 5 : 2
      }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output acrLoginServer string = acr.properties.loginServer
output apiUrl string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output keyVaultUri string = keyVault.properties.vaultUri
output openAiEndpoint string = openAI.properties.endpoint
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output redisFqdn string = redis.properties.hostName
output storageAccountName string = storage.name
output serviceBusNamespace string = serviceBus.properties.serviceBusEndpoint
