# Deployment Implementation — Phase 9

## Files created

### Bicep IaC

| File | Purpose |
|---|---|
| `infra/main.bicep` | Orchestrator — deploys all 5 modules in dependency order |
| `infra/modules/appinsights.bicep` | Log Analytics Workspace + Application Insights (workspace-based) |
| `infra/modules/keyvault.bicep` | Key Vault Standard, RBAC mode, KV Secrets User role for App Service MI |
| `infra/modules/sql.bicep` | SQL Server (Entra-only auth, TLS 1.2) + GP Serverless DB + Azure Services firewall |
| `infra/modules/storage.bicep` | Storage Account Standard LRS, no public blob access, Blob Data Contributor for App Service MI |
| `infra/modules/appservice.bicep` | App Service Plan Linux B2 + App Service (.NET 10), system-assigned MI, Key Vault references |

### Parameter files

| File | Environment |
|---|---|
| `infra/parameters/dev.bicepparam` | Dev — fully populated with placeholder values; 30-day log retention |
| `infra/parameters/qa.bicepparam` | QA skeleton |
| `infra/parameters/staging.bicepparam` | Staging skeleton — 90-day log retention |
| `infra/parameters/prod.bicepparam` | Prod skeleton — 90-day log retention |

### GitHub Actions

| Workflow | Trigger |
|---|---|
| `.github/workflows/ci.yml` | Push to main/develop, PR to main — build, test, Bicep lint, publish artifact |
| `.github/workflows/cd-dev.yml` | Auto after CI succeeds on main; manual dispatch — infra deploy + app deploy + smoke test |
| `.github/workflows/cd-qa.yml` | Manual dispatch (tag/SHA input) |
| `.github/workflows/cd-staging.yml` | Manual dispatch (tag/SHA input) |
| `.github/workflows/cd-prod.yml` | Manual dispatch with `DEPLOY` confirmation gate + what-if preview |

### Documentation

- `docs/runbook.md` — first-time bootstrap, Key Vault secret population, SQL contained user setup, EF migration command, rollback procedure, pre-production security checklist

---

## Architecture alignment

All resources deployed to `southafricanorth` — satisfies data residency requirement (POPIA/GDPR, no data outside SA).

| Spec requirement | Implementation |
|---|---|
| Entra-only SQL auth | `azureADOnlyAuthentication: true`, no SA password |
| TLS 1.2 minimum | `minimalTlsVersion: '1.2'` on SQL; `minTlsVersion: '1.2'` on App Service |
| HTTPS only | `httpsOnly: true` on App Service |
| No secrets in source control | All secrets via Key Vault references; connection string is `@Microsoft.KeyVault(...)` |
| Managed Identity access | System-assigned MI on App Service; KV Secrets User + Blob Data Contributor roles assigned |
| FTPS disabled | `ftpsState: 'Disabled'` |
| App Insights | Workspace-based; connection string set as app setting (not instrumentation key) |
| Key Vault RBAC mode | `enableRbacAuthorization: true` — no legacy access policies |

---

## Deployment module order (main.bicep)

1. `monitoring` — App Insights + Log Analytics (no dependencies)
2. `appservice` — App Service Plan + App Service (outputs `appServicePrincipalId` needed by steps 3–5)
3. `keyvault` — depends on `appservice.outputs.appServicePrincipalId`
4. `sql` — parallel with keyvault (both depend only on appservice)
5. `storage` — parallel with keyvault and sql

---

## Post-deploy manual steps (dev bootstrap only)

1. Add federated GitHub Actions OIDC credential to the service principal (see `docs/runbook.md §1.1`)
2. Set 5 GitHub repository secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `SQL_ADMIN_OBJECT_ID`, `SQL_ADMIN_LOGIN`)
3. Populate 3 Key Vault secrets after first deployment (`ConnectionStrings--DefaultConnection`, `AzureAd--TenantId`, `AzureAd--ClientId`)
4. Grant App Service managed identity as SQL contained user (see `docs/runbook.md §7`)
5. Run `dotnet ef database update` to apply initial migration

---

## Open items (non-blocking for this phase)

1. **Entra ID app registration not yet created** — `AzureAd__ClientId` and `AzureAd__TenantId` Key Vault secrets are placeholders. Must be populated before Entra OIDC wiring (security finding S-006).
2. **SQL DENY on AuditEvents** — `DENY UPDATE, DELETE ON AuditEvents TO PUBLIC` must be run as a one-off SQL script post-migration; this cannot be expressed in an EF Core migration without raw SQL.
3. **Application Insights alerting** — Alert rules per spec §5.6 not yet configured (no ARM/Bicep for Azure Monitor alert rules in this phase — add in a follow-on IaC PR).
4. **Entra Conditional Access policy for MFA** — configured in Entra Admin Center, not in Bicep. Document in runbook iteration 2.
5. **S-001/S-002/S-003** — security findings from Phase 8; must be resolved before production deployment. See `docs/runbook.md §9`.

VERDICT: APPROVED
