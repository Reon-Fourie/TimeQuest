@description('Environment name: dev, qa, staging, prod')
param environmentName string = 'dev'

@description('Azure region — all resources in South Africa North per data residency requirement')
param location string = 'southafricanorth'

@description('Entra ID object ID of the SQL administrator user or group')
param sqlAdminObjectId string

@description('Display name of the Entra ID SQL admin')
param sqlAdminLogin string

@description('Log Analytics retention in days')
param logRetentionDays int = 30

// ── Phase 1: App Insights + Log Analytics (no dependencies) ─────────────────

module monitoring 'modules/appinsights.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    environmentName: environmentName
    logRetentionDays: logRetentionDays
  }
}

// ── Phase 2: App Service Plan + App Service (system MI needed by later modules)

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    location: location
    environmentName: environmentName
    // keyVaultUri is set after Key Vault is created; use a placeholder for the
    // first deployment — Key Vault references are resolved at runtime, not deploy time.
    keyVaultUri: 'https://kv-timequest-${environmentName}-san.vault.azure.net/'
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    // SQL server FQDN follows a deterministic naming convention; no circular dep.
    sqlServerFqdn: 'sql-timequest-${environmentName}-san.database.windows.net'
    sqlDatabaseName: 'sqldb-timequest-${environmentName}-san'
    storageAccountName: 'sttimequest${environmentName}san'
  }
}

// ── Phase 3: Key Vault (requires App Service principal ID) ───────────────────

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    environmentName: environmentName
    appServicePrincipalId: appservice.outputs.appServicePrincipalId
  }
}

// ── Phase 4: SQL Server + Database ───────────────────────────────────────────

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    environmentName: environmentName
    sqlAdminObjectId: sqlAdminObjectId
    sqlAdminLogin: sqlAdminLogin
    appServicePrincipalId: appservice.outputs.appServicePrincipalId
  }
}

// ── Phase 5: Storage Account ─────────────────────────────────────────────────

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    environmentName: environmentName
    appServicePrincipalId: appservice.outputs.appServicePrincipalId
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

@description('App Service URL')
output appUrl string = 'https://${appservice.outputs.appServiceHostname}'

@description('Key Vault name')
output keyVaultName string = keyvault.outputs.keyVaultName

@description('Key Vault URI')
output keyVaultUri string = keyvault.outputs.keyVaultUri

@description('SQL Server FQDN')
output sqlServerFqdn string = sql.outputs.sqlServerFqdn

@description('SQL Database name')
output sqlDatabaseName string = sql.outputs.sqlDatabaseName

@description('Storage account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('App Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
