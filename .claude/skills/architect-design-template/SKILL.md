---
name: architect-design-template
description: The canonical design.md template for the System Architect agent (phase 2). Provides all 8 section headers and inline guidance on what each section must contain. Invoke this skill ONLY when writing or rewriting design.md - it is not needed while reasoning about service selection.
---

# Architect Design Template

Use exactly these section headers. The Architect Critic gates on this structure - renaming or renumbering blocks.

```markdown
# Architecture - <Project Name>

## 1. Component diagram (text)
<Mermaid `flowchart LR` or ASCII boxes showing the request path: Browser -> Blazor Server -> ASP.NET API -> DB, plus ancillaries (Key Vault, App Insights, Storage, ...). Keep to one diagram.>

## 2. Azure services chosen
| Service | Purpose | SKU (dev) | Est. monthly cost (dev) |
|---|---|---|---|
| ... | ... | ... | ~$N |

Total dev env cost: ~$N/month

## 3. Rejected alternatives (with reasons)
- **<Alternative> over <Chosen>** - rejected: <one-line reason tied to spec or NFR>
- ...

## 4. Project structure
\`\`\`
src/
  <Project>.Web/         # Blazor Web App
  <Project>.Api/         # ASP.NET API (only if separated from Web)
  <Project>.Domain/      # Entities, interfaces
  <Project>.Infrastructure/  # EF Core, external integrations
  <Project>.Shared/      # DTOs reused by Web + Api
tests/
  <Project>.UnitTests/
  <Project>.IntegrationTests/
  <Project>.E2E/         # Playwright
\`\`\`
Justify split vs single-project (default: single project when ≤5 features and one team).

## 5. Cross-cutting concerns
- **AuthN/AuthZ:** <chosen mechanism + why> - matches §5.4 of the spec
- **Logging / telemetry:** App Insights, connection string from Key Vault, structured JSON logs
- **Config:** appsettings + Key Vault references (App Configuration only if multi-env complexity demands it)
- **Secrets:** Azure Key Vault, accessed via Managed Identity
- **Health checks:** `/healthz` endpoint, probed by App Service
- **HTTPS / TLS:** HTTPS-only, min TLS 1.2 (or as §5.4 specifies)

## 6. Environments
- **dev** (always-on, small SKU, shared)
- **qa** (future) - same shape as dev, separate resources
- **staging** (future) - production-shape, separate resources
- **prod** (future) - production SKUs
List which resources are per-env vs shared (e.g. ACR shared, Log Analytics workspace shared).

## 7. Non-functional decisions
Tie each decision back to the matching §5.x NFR from the spec:
- **Caching:** in-memory / distributed / none - rationale tied to §5.1 (Performance) and §5.2 (Scalability)
- **Background jobs:** hosted service / Azure Functions / Service Bus + worker - rationale tied to specific features
- **File storage:** Blob / DB-as-bytes / none - rationale tied to feature needs
- **Multi-region:** yes / no - rationale tied to §5.2 and §5.3
- **DR / backups:** rationale tied to §5.3 (RTO/RPO)
- **Compliance posture:** regime named in §5.4 -> specific Azure controls (Private Endpoints, CMK, audit log export, etc.)

## 8. Open questions for Data Designer
- <items the data layer needs to resolve based on the architecture decisions>
- <any unresolved choice that the Data phase can settle without re-opening the architecture>

## 9. Changelog (iteration deltas)
- Iteration 1: initial design
- Iteration 2 (if any): <what changed and why in response to critic-1.md>
```

## Section-numbering invariants
- §1-§3: services + rejected alternatives (the "what")
- §4: project structure
- §5-§6: cross-cutting + environments
- §7: NFR-aligned decisions (must reference §5.x of the spec)
- §8: questions for downstream phases
- §9: iteration changelog (omit on iteration 1)

The Architect Critic gates on this structure.
