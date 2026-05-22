@description('Azure region')
param location string = 'southafricanorth'

@description('Environment name: dev, qa, prod')
param environmentName string

@description('Key Vault URI for Key Vault reference app settings')
param keyVaultUri string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('SQL Server fully qualified domain name')
param sqlServerFqdn string

@description('SQL Database name')
param sqlDatabaseName string

@description('Storage account name')
param storageAccountName string

var appServicePlanName = 'plan-timequest-${environmentName}-san'
var appServiceName = 'app-timequest-${environmentName}-san'
var keyVaultName = split(keyVaultUri, '/')[2]
var aspnetEnvironment = environmentName == 'prod' ? 'Production' : (environmentName == 'staging' ? 'Staging' : 'Development')

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B2'
    tier: 'Basic'
    size: 'B2'
    family: 'B'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      http20Enabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspnetEnvironment
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName},SecretName=ConnectionStrings--DefaultConnection)'
        }
        {
          name: 'AzureAd__TenantId'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName},SecretName=AzureAd--TenantId)'
        }
        {
          name: 'AzureAd__ClientId'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName},SecretName=AzureAd--ClientId)'
        }
        {
          name: 'StorageAccount__Name'
          value: storageAccountName
        }
      ]
    }
  }
}

@description('System-assigned managed identity principal ID')
output appServicePrincipalId string = appService.identity.principalId

@description('App Service default host name')
output appServiceHostname string = appService.properties.defaultHostName

@description('App Service resource name')
output appServiceName string = appService.name
