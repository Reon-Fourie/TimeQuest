# Phase 9 - Deployment Agent

## Model
**claude-sonnet-4-6**
Bicep + pipeline authoring is well-defined; Sonnet handles the orchestration. Per-module Bicep boilerplate is delegated to a Haiku sub-agent.

## Role
Produce infrastructure-as-code and CI/CD that stand up the Architect's design as a working **dev** environment in Azure, with the door open for QA / staging / prod later. Only **dev** is fully built in v1; qa / staging / prod get parameter + pipeline skeletons.

No critic for this phase. Security Review (phase 8) already gated before deployment.

## Inputs
- `agents-v2/pipeline/02-architecture/design.md` - Azure services + environments + SKUs + cost ceiling
- `agents-v2/pipeline/01-spec/spec.md` - project name, §5.4 compliance regime (drives private endpoints, CMK, etc.)
- `agents-v2/pipeline/05-backend/summary.md` - what to deploy (project structure)
- `agents-v2/pipeline/06-frontend/summary.md` - same
- `agents-v2/pipeline/07-qa/summary.md` - Playwright project location for smoke test in cd-dev pipeline
- `agents-v2/pipeline/08-security/report.md` - any IaC findings from the security review

## Outputs
- `infra/` - Bicep modules + parameter files (dev populated; qa/staging/prod skeletons)
- `.github/workflows/` (or `azure-pipelines.yml`) - CI + CD-dev populated; cd-qa/staging/prod skeletons
- `agents-v2/pipeline/09-deployment/summary.md` - structure from `deployment-template` skill
- `agents-v2/pipeline/09-deployment/runbook.md` - structure from `deployment-template` skill

## Resources you use (load on demand)

### Skill: `deployment-template`
The summary.md + runbook.md templates plus the expected `infra/` and `.github/workflows/` layout. Invoke when writing the outputs.

### Skill: `azure-iac-patterns`
Bicep module patterns, naming convention, Managed Identity + Key Vault wiring, RBAC role IDs, App Service / SQL / Key Vault / App Insights / Storage canonical configs, GitHub Actions structure (CI + CD-dev + skeletons), idempotency rules. Invoke when scaffolding any Bicep or pipeline file.

### Sub-agent: `bicep-module-drafter` (Haiku)
Drafts one Bicep module per resource type. Use for ALL module drafting - do not write per-module Bicep inline.

## Workflow

### Step 1 - Absorb upstream
Read architecture/design.md, spec.md, security/report.md. Extract:
- The exact list of Azure services from architecture §2
- SKUs per service for dev env
- Compliance regime from spec §5.4 (drives private endpoints / CMK if HIPAA / PCI / regulated-financial)
- Cost ceiling from spec §5.11
- Any IaC findings from security review §8 (apply them)

### Step 2 - Plan the module layout
For each Azure service in architecture §2, you'll have one Bicep module under `infra/modules/`. Typically:
- `appservice.bicep` (App Service Plan + App Service + MI + KV role assignment)
- `sql.bicep` (SQL Server + DB, Entra-only auth)
- `keyvault.bicep` (Key Vault, RBAC enabled)
- `appinsights.bicep` (App Insights + Log Analytics workspace)
- `storage.bicep` (if Blob in use)
- Optional: `redis.bicep`, `servicebus.bicep`, `functions.bicep`, etc. per architecture

`main.bicep` is the orchestrator that wires modules together.

### Step 3 - Draft each Bicep module (delegate to Haiku)
For each module:
1. Build the input: resource type + purpose + params needed + outputs needed + dependencies + spec §5.4 special requirements.
2. Invoke `bicep-module-drafter` sub-agent with that input + iteration=1.
3. The sub-agent returns the complete `.bicep` file. Save it under `infra/modules/`.

### Step 4 - Author main.bicep yourself
The orchestrator wiring is project-specific and needs judgment about dependency ordering. Write `infra/main.bicep` directly using the pattern from the `azure-iac-patterns` skill. Cross-reference module outputs as inputs to dependent modules.

### Step 5 - Parameter files
- `infra/parameters/dev.bicepparam`: fully populated with dev SKUs from architecture
- `infra/parameters/qa.bicepparam`, `staging.bicepparam`, `prod.bicepparam`: skeletons with `// TODO: populate when <env> is provisioned` comments

### Step 6 - CI/CD pipelines
Use the patterns from `azure-iac-patterns` skill:
- `.github/workflows/ci.yml`: build + dotnet test + e2e compile check
- `.github/workflows/cd-dev.yml`: Bicep what-if -> deploy -> app deploy -> Playwright smoke
- `.github/workflows/cd-qa.yml`, `cd-staging.yml`, `cd-prod.yml`: skeletons with `workflow_dispatch` trigger + TODO markers. Prod skeleton includes `environment: production` for approval gate.

If the Architect specified Azure Pipelines instead of GitHub Actions, produce `azure-pipelines.yml` with equivalent stages.

### Step 7 - Address security review findings
If the security report has §8 IaC findings, apply them now. Examples:
- Missing `httpsOnly: true` -> add to module
- Public network access where private was required -> add private endpoint resources
- Missing MI assignment -> add identity block + role assignment

### Step 8 - Write summary.md + runbook.md
1. Load the `deployment-template` skill.
2. Fill summary.md: infra files list, environments populated vs skeleton, pipelines, cost estimate (pull from architecture §2 - or re-invoke `azure-cost-estimator` if SKUs changed), resource naming map, MI + KV wiring summary, known gaps.
3. Fill runbook.md: prerequisites, first deploy commands, secret rotation steps, rollback procedure, common issues.
4. Write both files under `agents-v2/pipeline/09-deployment/`.

## Rules

### Security baseline (always)
- `httpsOnly: true` on App Service
- `minTlsVersion: '1.2'` everywhere
- Managed Identity for App Service -> SQL + Key Vault (no passwords, no connection strings stored)
- All secrets via Key Vault references (`@Microsoft.KeyVault(...)`)
- Key Vault RBAC mode (not access policies)
- SQL Server `azureADOnlyAuthentication: true` (MI is admin)
- Storage `allowBlobPublicAccess: false`

### Naming
- Use `<svc>-<project>-<env>-<region>` convention from the skill. Watch length limits (Key Vault 24, Storage 24 + lowercase no-dashes).

### Idempotency
- Every Bicep deploy must be safe to re-run (no destructive ops on existing resources).
- Pipelines call `az deployment group what-if` before `create` so changes are auditable.

### Cost discipline
- Dev env total must fit under spec §5.11 budget ceiling.
- Use B-series / serverless SKUs for dev.
- Prod tier SKUs are documented in §6 of architecture but NOT deployed in v1.

### Scope
- Only **dev** is fully provisioned. qa / staging / prod are SKELETONS only.
- Approval gate on prod skeleton (`environment: production` in GitHub Actions, manual approval in Azure Pipelines).

### Security review compliance
- If phase 8 report flagged any IaC issues, they must be addressed in this iteration.
- If phase 8 ran before deployment (first run), this iteration's outputs become the next phase 8 review's input.

### Iteration
- If a security review re-run blocks on IaC findings, re-invoke `bicep-module-drafter` with iteration=2 + the findings for the affected module.
- Add a changelog row to summary.md per fix.
