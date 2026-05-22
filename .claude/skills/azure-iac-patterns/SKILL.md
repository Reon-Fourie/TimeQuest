---
name: azure-iac-patterns
description: Azure Bicep and CI/CD pipeline patterns for the Deployment agent (phase 9). Covers Bicep module structure, resource naming convention, Managed Identity + Key Vault wiring, RBAC role assignments, App Service config (HTTPS-only, TLS 1.2, slots), Azure SQL config (Entra auth, firewall, private endpoint), GitHub Actions / Azure Pipelines structure (CI, CD-dev with Bicep what-if + smoke test, env-specific skeletons), and idempotency rules. Invoke when scaffolding infra/ or pipelines.
---

# Azure IaC + Pipeline Patterns

## Bicep module structure

### main.bicep (orchestrator)

```bicep
targetScope = 'resourceGroup'

@description('Project short name, e.g. timequest')
param projectName string

@description('Environment, e.g. dev, qa, staging, prod')
param environment string

@description('Azure region')
param location string = resourceGroup().location

@description('Region short code for naming, e.g. weu')
param locationShort string = 'weu'

@description('App Service SKU')
param appServiceSku string = 'B1'

var namePrefix = '${projectName}-${environment}-${locationShort}'

module appInsights './modules/appinsights.bicep' = {
  name: 'deploy-appinsights'
  params: {
    name: 'appi-${namePrefix}'
    location: location
  }
}

module keyVault './modules/keyvault.bicep' = {
  name: 'deploy-keyvault'
  params: {
    name: 'kv-${take(namePrefix, 21)}'  // 24-char max
    location: location
  }
}

module sql './modules/sql.bicep' = {
  name: 'deploy-sql'
  params: {
    serverName: 'sql-${namePrefix}'
    dbName: 'sqldb-${namePrefix}'
    location: location
  }
}

module app './modules/appservice.bicep' = {
  name: 'deploy-app'
  params: {
    appName: 'app-${namePrefix}'
    planName: 'plan-${namePrefix}'
    sku: appServiceSku
    location: location
    keyVaultName: keyVault.outputs.name
    appInsightsConnectionString: appInsights.outputs.connectionString
    sqlServerFqdn: sql.outputs.serverFqdn
    sqlDbName: sql.outputs.dbName
  }
}

output appUrl string = app.outputs.url
output keyVaultUri string = keyVault.outputs.uri
```

### Modules (one resource per file)

```bicep
// modules/appservice.bicep
param appName string
param planName string
param sku string
param location string
param keyVaultName string
param appInsightsConnectionString string
param sqlServerFqdn string
param sqlDbName string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: { name: sku }
  kind: 'linux'
  properties: { reserved: true }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'ConnectionStrings__DefaultConnection', value: '@Microsoft.KeyVault(SecretUri=https://${keyVaultName}.vault.azure.net/secrets/SqlConnectionString/)' }
      ]
    }
  }
}

resource appKvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: resourceGroup()
  name: guid(app.id, 'kv-secrets-user')
  properties: {
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')  // Key Vault Secrets User
  }
}

output url string = 'https://${app.properties.defaultHostName}'
output principalId string = app.identity.principalId
```

## Resource naming convention

Format: `<service-abbreviation>-<project>-<env>-<region>`

| Service | Abbreviation | Example |
|---|---|---|
| App Service | `app` | `app-timequest-dev-weu` |
| App Service Plan | `plan` | `plan-timequest-dev-weu` |
| Azure SQL Server | `sql` | `sql-timequest-dev-weu` |
| Azure SQL DB | `sqldb` | `sqldb-timequest-dev-weu` |
| Key Vault | `kv` | `kv-timequest-dev-weu` (24-char max) |
| App Insights | `appi` | `appi-timequest-dev-weu` |
| Storage Account | (none, lowercase, no dashes) | `sttimequestdevweu` (24-char max) |
| Container Registry | `cr` | `crtimequestdevweu` (no dashes for ACR) |
| Container Apps Env | `cae` | `cae-timequest-dev-weu` |
| Function App | `func` | `func-timequest-dev-weu` |
| Log Analytics | `log` | `log-timequest-dev-weu` |
| Resource Group | `rg` | `rg-timequest-dev` (no region needed) |

Length limits to remember:
- Storage account: 24 chars, lowercase, no dashes
- Key Vault: 24 chars
- App Service: 60 chars
- SQL Server: 63 chars

## Managed Identity + Key Vault pattern

1. App Service has System-Assigned Managed Identity (`identity: { type: 'SystemAssigned' }`)
2. Grant MI the `Key Vault Secrets User` role on the Key Vault (NOT access policies - use RBAC)
3. App settings use Key Vault references for secrets:
   ```
   ConnectionStrings__DefaultConnection = @Microsoft.KeyVault(SecretUri=https://kv-foo.vault.azure.net/secrets/SqlConnectionString/)
   ```
4. App Service auto-refreshes references (~24h cache; restart to force).
5. For SQL: MI is the SQL admin. App connects with `Authentication=Active Directory Default` - no password ever stored.

## Azure SQL pattern

```bicep
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: 'app-${projectName}-${env}-mi'
      sid: appPrincipalId  // App Service MI is SQL admin
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    publicNetworkAccess: 'Enabled'  // 'Disabled' if §5.4 requires private
    minimalTlsVersion: '1.2'
  }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: dbName
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
}

resource firewallAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}
```

## Key Vault pattern

```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    enableRbacAuthorization: true       // RBAC, not access policies
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    softDeleteRetentionInDays: 7        // 7 for dev, 90 for prod
    publicNetworkAccess: 'Enabled'      // 'Disabled' with PE if §5.4 requires
  }
}

output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
```

## App Insights pattern

```bicep
resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${namePrefix}'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

output connectionString string = appInsights.properties.ConnectionString
```

## Parameter files

### dev.bicepparam (fully populated)
```bicep
using './main.bicep'

param projectName = 'timequest'
param environment = 'dev'
param location = 'westeurope'
param locationShort = 'weu'
param appServiceSku = 'B1'
```

### qa/staging/prod.bicepparam (skeletons)
```bicep
using './main.bicep'

// TODO: populate when qa env is needed
param projectName = 'timequest'
param environment = 'qa'
param location = 'westeurope'
param locationShort = 'weu'
param appServiceSku = 'B2'  // typically same or one tier up from dev
```

Prod uses production tiers (P1v3, S0 / GP_S_Gen5_1 etc.) per architect's design.

## GitHub Actions structure

### ci.yml

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --verbosity normal

  e2e-compile:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd tests/e2e && npm ci && npx tsc --noEmit
```

### cd-dev.yml

```yaml
name: CD (dev)

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  id-token: write  # OIDC
  contents: read

jobs:
  provision:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      - name: Bicep what-if
        run: |
          az deployment group what-if \
            -g rg-timequest-dev \
            -f infra/main.bicep \
            -p infra/parameters/dev.bicepparam
      - name: Bicep deploy
        run: |
          az deployment group create \
            -g rg-timequest-dev \
            -f infra/main.bicep \
            -p infra/parameters/dev.bicepparam

  deploy-app:
    runs-on: ubuntu-latest
    needs: provision
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      - run: dotnet publish src/TimeQuest.Web -c Release -o publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: app-timequest-dev-weu
          package: publish

  smoke:
    runs-on: ubuntu-latest
    needs: deploy-app
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd tests/e2e && npm ci && npx playwright install --with-deps
      - run: cd tests/e2e && BASE_URL=https://app-timequest-dev-weu.azurewebsites.net npx playwright test smoke
```

### cd-qa.yml / cd-staging.yml / cd-prod.yml (skeletons)

```yaml
name: CD (qa)
on:
  workflow_dispatch:  # manual trigger for now
# TODO: enable push trigger when qa env is provisioned
# TODO: copy cd-dev.yml structure, adjust env names + parameter files
```

Prod skeleton adds an approval gate:
```yaml
jobs:
  approval:
    runs-on: ubuntu-latest
    environment:
      name: production  # configured with required reviewers in repo settings
    steps:
      - run: echo "Approved"
```

## Pipeline idempotency

- Every Bicep deploy MUST be idempotent (re-running produces same state).
- App deploys use slot-swap when slots exist (otherwise direct deploy).
- DB migrations are forward-only with safe `Up` scripts; never modify deployed migrations.
- Pipeline runs use `--only-show-errors` to keep logs clean, but real errors must bubble up.

## What NOT to do

- No `az group create` in pipelines - assume RG exists (created manually first time)
- No `secrets.<NAME>` for the SQL password - there is no SQL password (Entra-only)
- No `AllowAllAzureIps` firewall rules in prod (it allows all Azure tenants, not just yours)
- No hardcoded subscription IDs / tenant IDs in Bicep - parameterize
- No `latest` tag in image references - pin versions
