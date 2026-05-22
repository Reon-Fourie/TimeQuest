---
name: deployment-template
description: The summary.md and runbook.md templates the Deployment agent (phase 9) writes, plus the expected layout for infra/ (Bicep) and .github/workflows/ (CI/CD YAML). Invoke ONLY when writing the deployment outputs.
---

# Deployment Templates

## Expected file layout

```
infra/
  main.bicep                        # top-level orchestrator
  modules/
    appservice.bicep                # App Service + Plan
    sql.bicep                       # Azure SQL server + DB
    keyvault.bicep                  # Key Vault + access policies
    appinsights.bicep               # Application Insights
    storage.bicep                   # Blob (if used)
  parameters/
    dev.bicepparam                  # fully populated
    qa.bicepparam                   # skeleton with TODO markers
    staging.bicepparam              # skeleton
    prod.bicepparam                 # skeleton

.github/workflows/                  # OR azure-pipelines.yml - pick one
  ci.yml                            # build + unit/integration tests
  cd-dev.yml                        # provision (what-if -> deploy) + app deploy + smoke
  cd-qa.yml                         # skeleton
  cd-staging.yml                    # skeleton
  cd-prod.yml                       # skeleton with approval gate
```

## summary.md template

```markdown
# Deployment Implementation - Iteration <N>

## Infra (Bicep)
- infra/main.bicep
- infra/modules/appservice.bicep
- infra/modules/sql.bicep
- infra/modules/keyvault.bicep
- infra/modules/appinsights.bicep
- infra/parameters/dev.bicepparam (populated)
- infra/parameters/{qa,staging,prod}.bicepparam (skeletons, TODO markers)

## Environments
- **dev**: fully populated; pipeline auto-deploys on merge to main
- **qa**, **staging**, **prod**: parameter skeletons + pipeline skeletons with TODO markers
- Approval gate on prod pipeline

## Pipelines (.github/workflows or azure-pipelines.yml)
- ci.yml: build + dotnet test + Playwright spec compile check
- cd-dev.yml: Bicep what-if -> apply -> app deploy -> Playwright smoke test
- cd-qa.yml / cd-staging.yml / cd-prod.yml: skeleton

## First-time setup
See runbook.md.

## Cost estimate (dev env)
(from architect §2 + azure-cost-estimator sub-agent)
- App Service B1: ~$13
- Azure SQL Basic: ~$5
- Key Vault: ~$0.03
- App Insights (basic): ~$3
- Total: ~$22/month

## Resource naming
Convention: `<service>-<project>-<env>-<region>`
- App Service: `app-<project>-dev-weu`
- SQL Server: `sql-<project>-dev-weu`
- Key Vault: `kv-<project>-dev-weu` (24-char max)
- App Insights: `appi-<project>-dev-weu`
- Storage: `st<project>devweu` (no dashes, 24-char max, lowercase)

## Managed Identity + Key Vault wiring
- App Service has a system-assigned Managed Identity
- MI granted `Key Vault Secrets User` role on the Key Vault
- App settings reference KV via `@Microsoft.KeyVault(SecretUri=https://kv-.../secrets/SqlConnectionString/)`
- SQL Server has Entra-only auth; the MI is the SQL admin (no SQL admin password stored anywhere)

## Known gaps
- <items deferred to next iteration>

## Changelog (iteration >= 2 only)
- Fix 1: <what changed>
```

## runbook.md template

```markdown
# Deployment Runbook

## Prerequisites
- Azure CLI logged in (`az login`)
- Subscription set (`az account set --subscription <id>`)
- Resource group exists (`az group create -n rg-<project>-dev -l westeurope`)
- Bicep CLI installed (`az bicep install`)

## First deploy (dev)

### 1. Create service principal for CI
\`\`\`bash
az ad sp create-for-rbac --name sp-<project>-cicd --role Contributor \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-<project>-dev \
  --json-auth
\`\`\`

Capture the JSON output - this is the value for `AZURE_CREDENTIALS` secret.

### 2. Add GitHub secrets (Repo Settings -> Secrets and variables -> Actions)
- `AZURE_CREDENTIALS`: JSON from step 1
- `AZURE_SUBSCRIPTION_ID`: your subscription ID
- `AZURE_RG_DEV`: `rg-<project>-dev`

### 3. First-time infra deploy (manual)
\`\`\`bash
az deployment group create \
  -g rg-<project>-dev \
  -f infra/main.bicep \
  -p infra/parameters/dev.bicepparam
\`\`\`

### 4. Database migration
EF migrations run automatically on app startup (via SeedData or migration runner). For manual:
\`\`\`bash
dotnet ef database update --connection "$(az keyvault secret show --name SqlConnectionString --vault-name kv-<project>-dev-weu --query value -o tsv)"
\`\`\`

### 5. First app deploy
Push to main. The cd-dev workflow runs Bicep what-if -> apply -> app deploy -> Playwright smoke test.

## Roll back

### App rollback (preferred)
Use deployment slots:
- App Service has a `staging` slot.
- Production rollback = swap back to the prior slot.
\`\`\`bash
az webapp deployment slot swap -g rg-<project>-dev -n app-<project>-dev-weu --slot staging --target-slot production
\`\`\`

### Infra rollback
- `git revert` the Bicep change
- Re-run the cd-dev pipeline

## Secret rotation

### Application secrets in Key Vault
\`\`\`bash
az keyvault secret set --vault-name kv-<project>-dev-weu --name <SecretName> --value <NewValue>
\`\`\`
App Service auto-refreshes Key Vault references within ~24h. Force refresh with:
\`\`\`bash
az webapp restart -g rg-<project>-dev -n app-<project>-dev-weu
\`\`\`

### Service principal credentials
- Rotate `AZURE_CREDENTIALS` annually.
- Re-run `az ad sp credential reset --name sp-<project>-cicd --json-auth` and update the GitHub secret.

## Common issues

### "Bicep deployment failed: ResourceQuotaExceeded"
- Check subscription quota in Azure portal -> Subscriptions -> Usage + quotas.
- Either request increase or downgrade SKU in dev.bicepparam.

### "App Service won't start, ConnectionString error"
- Verify Managed Identity has Key Vault Secrets User role.
- Check App Service -> Configuration -> Connection strings has the KV reference syntax.
- App Service log stream: `az webapp log tail -g rg-<project>-dev -n app-<project>-dev-weu`

### "EF migration fails on first deploy"
- Check the MI has `db_owner` (or appropriate role) on SQL.
- Run migration manually from a machine with `az login` set.

## Cost monitoring
- Budget alert configured in Azure Cost Management at the spec §5.11 ceiling
- Action group emails the team on 80% threshold
```

## Bicep style conventions
- Use AVM (Azure Verified Modules) when they fit the use case
- Otherwise plain Bicep modules with explicit parameters
- All resource names parameterised (don't hardcode env-specific values in modules)
- Each module exports the IDs needed by main.bicep for cross-references
- Outputs include endpoint URLs that the runbook needs
