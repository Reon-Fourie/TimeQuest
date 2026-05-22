@description('Azure region')
param location string = 'southafricanorth'

@description('Environment name (dev, qa, prod)')
param environmentName string

@description('Log retention in days')
param logRetentionDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-timequest-${environmentName}-san'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionDays
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-timequest-${environmentName}-san'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

@description('Application Insights connection string')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Application Insights instrumentation key (legacy)')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Log Analytics Workspace resource ID')
output logAnalyticsWorkspaceId string = workspace.id
