# TimeQuest Deployment Runbook

## Prerequisites

- Azure CLI ≥ 2.60 (`az --version`)
- Bicep CLI ≥ 0.29 (installed via `az bicep install`)
- .NET 10 SDK
- GitHub repository secrets configured (see §3)

---

## 1. First-time bootstrap (dev)

### 1.1 Create a service principal for GitHub Actions (OIDC)

```bash
# Substitute your subscription ID and resource group name
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RG=rg-timequest-dev-san

# Create federated identity credential for GitHub Actions OIDC
az ad app create --display-name "sp-timequest-github-actions"
APP_ID=$(az ad app list --display-name "sp-timequest-github-actions" --query "[0].appId" -o tsv)
az ad sp create --id $APP_ID
SP_OBJECT_ID=$(az ad sp show --id $APP_ID --query id -o tsv)

az role assignment create \
  --role Contributor \
  --assignee $SP_OBJECT_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG

az role assignment create \
  --role "User Access Administrator" \
  --assignee $SP_OBJECT_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG
```

Add federated credentials for GitHub Actions via the Azure Portal:
- App registration → Certificates & secrets → Federated credentials → Add
- Scenario: GitHub Actions deploying Azure resources
- Organisation: `<your-github-org>`, Repository: `TimeQuest`, Entity: Branch `main`

### 1.2 Set GitHub repository secrets

In GitHub → Settings → Secrets and variables → Actions:

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | App registration Application (client) ID |
| `AZURE_TENANT_ID` | Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `SQL_ADMIN_OBJECT_ID` | Entra Object ID of the SQL admin user/group |
| `SQL_ADMIN_LOGIN` | UPN or display name of the SQL admin |

### 1.3 Populate Key Vault secrets post-deploy

After the first Bicep deployment creates the Key Vault (`kv-timequest-dev-san`), add these secrets manually:

```bash
KV=kv-timequest-dev-san
SQL_FQDN=sql-timequest-dev-san.database.windows.net
SQL_DB=sqldb-timequest-dev-san

# Connection string — Entra-only auth, no password
az keyvault secret set \
  --vault-name $KV \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=${SQL_FQDN};Database=${SQL_DB};Authentication=Active Directory Managed Identity;Encrypt=True;"

# Entra ID OIDC (replace with real values when Entra app registration is created)
az keyvault secret set --vault-name $KV --name "AzureAd--TenantId"  --value "<TENANT_ID>"
az keyvault secret set --vault-name $KV --name "AzureAd--ClientId"  --value "<CLIENT_ID>"
```

**Data residency check**: Confirm the Key Vault is in `southafricanorth` before populating.

---

## 2. Routine deployment (dev)

CI runs automatically on push to `main`. CD to dev triggers automatically after CI passes.

To trigger manually:
```
GitHub → Actions → "CD — Dev" → Run workflow
```

---

## 3. Promotion: dev → qa → staging → prod

Each environment requires a manual `workflow_dispatch` trigger with a git SHA or tag:

1. Verify the SHA passes all CI checks.
2. Go to GitHub → Actions → select the target workflow (e.g. "CD — QA").
3. Click "Run workflow", enter the SHA/tag.
4. For production: enter `DEPLOY` in the confirmation field.
5. Monitor the deployment job; check the smoke test step.

---

## 4. Production Key Vault secrets

Before any production deployment, populate prod Key Vault (`kv-timequest-prod-san`) with the same secret names as dev. Production Entra app registration must be a **separate** registration from dev.

Enable purge protection on the prod Key Vault post-creation:
```bash
az keyvault update \
  --name kv-timequest-prod-san \
  --enable-purge-protection true \
  --retention-days 90
```

---

## 5. Replacing `appsettings.json` connection string

The `TimeQuest/appsettings.json` file contains a LocalDB dev connection string:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TimeQuestDev;Trusted_Connection=True;"
```
This is intentionally left as a local dev fallback. Production App Services override this via the Key Vault reference app setting `ConnectionStrings__DefaultConnection` — the appsettings value is never read in Azure.

**To confirm this in any environment**: check App Service → Configuration → Application settings → `ConnectionStrings__DefaultConnection` shows the Key Vault reference, not the localdb string.

---

## 6. EF Core migrations

Run migrations against the Azure dev database using the Entra ID admin identity:

```powershell
# From repo root, with your Entra ID logged in to az CLI
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project TimeQuest -- --ConnectionStrings:DefaultConnection "Server=sql-timequest-dev-san.database.windows.net;Database=sqldb-timequest-dev-san;Authentication=Active Directory Interactive;Encrypt=True;"
```

For CI-driven migration (future): add a migration step to `cd-dev.yml` using a GitHub Actions runner with Entra federated identity.

---

## 7. Adding the App Service as a SQL contained user

After the App Service is deployed, run this SQL as the Entra admin to grant the managed identity access:

```sql
-- Connect to sqldb-timequest-dev-san as Entra admin
CREATE USER [app-timequest-dev-san] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-timequest-dev-san];
ALTER ROLE db_datawriter ADD MEMBER [app-timequest-dev-san];
-- Grant DDL for EF migrations (dev only; use a dedicated migration identity in prod)
ALTER ROLE db_ddladmin ADD MEMBER [app-timequest-dev-san];
```

---

## 8. Rollback procedure

1. Identify the last known-good git SHA.
2. Trigger the environment's CD workflow with that SHA.
3. If a bad migration was applied: run `dotnet ef database update <PreviousMigrationName>` against the DB before redeploying the old app version.
4. If infrastructure was broken: run `az deployment group create` with the last known-good parameter file + a backup copy of the template.

---

## 9. Security checklist before production go-live

- [ ] S-001: Team-scope check added to `ApprovalService.GetTimesheetDetailAsync`
- [ ] S-002: `GetCurrentUserId()` stubs replaced with `AuthenticationStateProvider`-based lookup
- [ ] S-003: Rate limiting added to export/report routes
- [ ] S-004: `appsettings.json` DefaultConnection value replaced with `""` or placeholder
- [ ] S-006: Entra ID OIDC wired via `AddMicrosoftIdentityWebApp`
- [ ] Key Vault purge protection enabled on prod vault
- [ ] `enablePurgeProtection: true` set in prod Bicep parameter override
- [ ] App Insights connection string stored in Key Vault (not in app settings plaintext)
- [ ] All services confirmed in `southafricanorth` (data residency check)
- [ ] SQL DENY UPDATE/DELETE on AuditEvents table applied (run once, not via EF migration)
